using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AiGameStudio.ArcadeControls;

namespace EndlessSisyphus.Tests
{
    /// <summary>
    /// Гард потолка скорости (жалоба основательницы «Сизиф улетает»): физическая крутилка на
    /// энкодере выдаёт сотни °/с, каждый набранный оборот превращается в толчок, и без потолка
    /// импульс камня растёт неограниченно.
    ///
    /// Тесты кодируют ИНВАРИАНТ, а не текущую цифру баланса:
    ///  - при сколь угодно быстром вращении импульс НИКОГДА не превышает GameConfig.MaxMomentum
    ///    (и подъём по высоте не обгоняет этот же потолок);
    ///  - медленное вращение до потолка не доходит вовсе — он режет только пик.
    /// Значение самого потолка основательница крутит в GameConfig, тесты от него не зависят.
    /// </summary>
    public class SpeedCapTests
    {
        GameObject host;
        SisyphusGame game;
        FakeBackend fake;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            host = new GameObject("SisyphusGame_SpeedCap");
            // Сброс бэкенда ДО Awake: адаптер пампит ввод только когда бэкенд его собственный,
            // иначе оставшийся от предыдущего теста бэкенд заморозит крутилку (см. ArcadeControlsAdapter).
            ArcadeInput.Initialize(null);
            game = host.AddComponent<SisyphusGame>();
            fake = new FakeBackend();
            ArcadeInput.Initialize(fake);
            yield return null;

            game.StartGame();
            game.IntroActive = false;              // пропустить вступительную анимацию
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

        // Спокойный участок без препятствий: изолируем чистую связку «крутилка → скорость».
        void PinCalm()
        {
            game.Obstacle = ObKind.None;
            game.Phase = ObPhase.Calm;
            game.ObTimer = 999f;
            game.IceActive = game.IsOnIce = false;
            game.SteepActive = game.IsOnSteep = false;
        }

        // Крутим seconds игрового времени с заданной скоростью крутилки, возвращаем пиковый импульс.
        IEnumerator Crank(float seconds, float degPerSecond, System.Action<float> onFrame)
        {
            float t = 0f;
            while (t < seconds && game != null && game.State == GState.Playing)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                t += dt;
                PinCalm();
                fake.Next = new BackendSnapshot { CrankDeltaDegrees = degPerSecond * dt };
                yield return null;
                onFrame(game.Momentum);
            }
        }

        [UnityTest]
        public IEnumerator HugeCrank_NeverExceedsSpeedCap()
        {
            float peak = float.MinValue;
            float startHeight = game.Height;
            float startTime = Time.time;

            // Абсурдно быстрое вращение (20000 °/с — многократно выше любого живого оборота):
            // потолок обязан удержать импульс независимо от того, сколько толчков пришло за кадр.
            yield return Crank(2f, 20000f, m => peak = Mathf.Max(peak, m));
            float elapsed = Time.time - startTime;

            Assert.AreEqual(GState.Playing, game.State, "на спокойном участке игрок не должен падать");
            Assert.LessOrEqual(peak, GameConfig.MaxMomentum + 1e-3f,
                "импульс камня обязан упираться в потолок GameConfig.MaxMomentum при любой скорости крутилки");
            Assert.Greater(peak, GameConfig.MaxMomentum * 0.9f,
                "крутилка должна доводить импульс до потолка — иначе тест ничего не проверяет");
            Assert.LessOrEqual(game.Height - startHeight, GameConfig.MaxMomentum * elapsed * 1.05f + 0.5f,
                "подъём по высоте не должен обгонять потолок скорости");
        }

        [UnityTest]
        public IEnumerator SlowCrank_StaysBelowCap_Untouched()
        {
            float peak = float.MinValue;

            // Медленное вращение (25 °/с ≈ один толчок за 0.8 с) физически не набирает потолок:
            // правка не должна трогать медленную игру.
            yield return Crank(2f, 25f, m => peak = Mathf.Max(peak, m));

            Assert.Less(peak, GameConfig.MaxMomentum,
                "медленное вращение обязано оставаться НИЖЕ потолка — потолок режет только пик");
        }
    }
}
