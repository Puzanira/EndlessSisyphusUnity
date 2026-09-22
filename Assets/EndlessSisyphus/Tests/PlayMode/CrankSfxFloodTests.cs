using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AiGameStudio.ArcadeControls;

namespace EndlessSisyphus.Tests
{
    /// <summary>
    /// Гард «звук выпадает по ходу игры» (живой плейтест на стойке).
    ///
    /// Причина, снятая в живом macOS-плеере: крутилка автомата выдаёт десятки толчков в
    /// секунду (клавиатурный оригинал автора — единицы), каждый толчок стрелял своим
    /// 0.26-секундным one-shot'ом, и пул реальных голосов Unity (m_RealVoiceCount = 32)
    /// переполнялся копиями одного клипа. Движок глушит наименее слышные голоса — а это
    /// музыка (0.4 против ~0.7 у толчков): при 2700 °/с дрон и мелодия уходили в isVirtual
    /// в 11 замерах из 12, при 4000 °/с — в 44 из 44, пик микса 3.56 (клиппинг).
    ///
    /// isVirtual headless не воспроизводится (в batchmode драйвер без звука), поэтому тесты
    /// пришпиливают две ПРИЧИНЫ, которые к этому вели, а не симптом:
    ///  1) поток толчковых SFX ограничен порогом «долбёжки» — сколько бы толчков крутилка
    ///     ни выдала, одновременно живущих копий клипа остаётся единицы;
    ///  2) музыка приоритетнее толчков — при нехватке голосов движку отдают толчок, а не её.
    /// </summary>
    public class CrankSfxFloodTests
    {
        GameObject host;
        SisyphusGame game;
        FakeBackend fake;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            host = new GameObject("SisyphusGame_SfxFlood");
            // Сброс бэкенда ДО Awake — см. комментарий в SpeedCapTests.
            ArcadeInput.Initialize(null);
            game = host.AddComponent<SisyphusGame>();
            fake = new FakeBackend();
            ArcadeInput.Initialize(fake);
            yield return null;

            game.StartGame();
            game.IntroActive = false;
            game.IntroT = GameConfig.IntroDuration;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (host != null) Object.Destroy(host);
            var world = GameObject.Find("EndlessSisyphus_World");
            if (world != null) Object.Destroy(world);
            yield return null;
        }

        void PinCalm()
        {
            game.Obstacle = ObKind.None;
            game.Phase = ObPhase.Calm;
            game.ObTimer = 999f;
            game.IceActive = game.IsOnIce = false;
            game.SteepActive = game.IsOnSteep = false;
        }

        [UnityTest]
        public IEnumerator MusicVoices_OutrankTapSfx()
        {
            Assert.Less(game.Audio.MusicVoicePriority, game.Audio.TapSfxVoicePriority,
                "музыка обязана быть приоритетнее толчков: при нехватке голосов движок " +
                "глушит наименее приоритетные, и без этого первой пропадала именно музыка");
            yield return null;
        }

        [UnityTest]
        public IEnumerator InsaneCrank_DoesNotFloodTheVoicePool()
        {
            int before = game.Audio.TapSfxPlayed;
            float startTime = Time.time;

            // Абсурдно быстрое вращение — многократно выше любого живого оборота.
            while (Time.time - startTime < 2f && game.State == GState.Playing)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                PinCalm();
                fake.Next = new BackendSnapshot { CrankDeltaDegrees = 20000f * dt };
                yield return null;
            }

            float elapsed = Time.time - startTime;
            int played = game.Audio.TapSfxPlayed - before;

            // Клип толчка звучит 0.26 с: при пороге «долбёжки» одновременно живут единицы
            // копий, а не десятки. Запас +2 — на границы кадров.
            int ceiling = Mathf.CeilToInt(elapsed / GameConfig.MashInterval) + 2;
            Assert.LessOrEqual(played, ceiling,
                "поток толчковых SFX обязан упираться в порог «долбёжки», иначе пул голосов " +
                "переполняется копиями одного клипа и движок глушит музыку");
            Assert.Greater(played, 0, "толчок обязан звучать — тест не должен проходить на тишине");
        }

        [UnityTest]
        public IEnumerator HumanRhythm_KeepsEveryPushAudible()
        {
            int before = game.Audio.TapSfxPlayed;

            // Человеческий ритм автора (IdealInterval 0.34 с) — прореживание не должно
            // съедать ни одного толчка, иначе правка испортила звук нормальной игры.
            const int taps = 5;
            for (int i = 0; i < taps; i++)
            {
                PinCalm();
                fake.Next = new BackendSnapshot
                {
                    CrankDeltaDegrees = GameConfig.CrankDegreesPerPush * 1.1f
                };
                yield return null;
                fake.Next = new BackendSnapshot();

                float waited = 0f;
                while (waited < GameConfig.IdealInterval)
                {
                    waited += Mathf.Min(Time.deltaTime, 0.05f);
                    PinCalm();
                    yield return null;
                }
            }

            Assert.AreEqual(taps, game.Audio.TapSfxPlayed - before,
                "на человеческом ритме обязан звучать КАЖДЫЙ толчок");
        }
    }
}
