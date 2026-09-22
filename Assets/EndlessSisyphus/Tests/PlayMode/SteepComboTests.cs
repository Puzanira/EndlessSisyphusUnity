using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AiGameStudio.ArcadeControls;

namespace EndlessSisyphus.Tests
{
    /// <summary>
    /// Гард главной механики крутого склона (живая жалоба основательницы после слияния
    /// июльского апдейта автора: «кнопка ! не проходит крутой склон»).
    ///
    /// Инвариант из комментария у GameConfig.SteepCorrectGravityMul: ПРАВИЛЬНАЯ комбинация
    /// (зажатый «!» + вращение) гарантированно преодолевает крутой участок. Тест играет так,
    /// как игру объясняет сама игра: баннер «Крутой склон. Зажми и держи жёлтую кнопку и
    /// продолжай крутить.» загорается ЗАРАНЕЕ
    /// (на подъезде), игрок по нему зажимает «!» и продолжает крутить — и должен пройти,
    /// а не сгорать по силам на подъезде. Контроль-тест рядом следит, что штраф автора за
    /// подъём БЕЗ «!» при этом жив.
    /// </summary>
    public class SteepComboTests
    {
        GameObject host;
        SisyphusGame game;
        FakeBackend fake;

        float patchStart, patchLen;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            host = new GameObject("SisyphusGame_SteepCombo");
            // Сброс бэкенда ДО Awake: адаптер пампит ввод только когда бэкенд его собственный.
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

        float SisX => game.Scroll + game.VW * 0.40f;

        // Крутой участок ровно так, как его ставит сама игра (StartSteep): участок появляется
        // на дистанции SteepLeadDist и подъезжает к игроку. Игрок видит его и баннер заранее
        // и зажимает «!» ДО въезда — так игра сама и учит («Крутой склон. Зажми и держи
        // жёлтую кнопку и продолжай крутить.»).
        void PlaceSteepAhead()
        {
            patchStart = SisX + game.VW * GameConfig.SteepLeadDist;
            patchLen = GameConfig.SteepLenMin;
            game.SteepStart = patchStart;
            game.SteepLen = patchLen;
            game.SteepActive = true;
            game.SteepPatches.Add(new Vector2(patchStart, patchLen));
            game.Obstacle = ObKind.Steep;
            game.Phase = ObPhase.Active;
            game.ObTimer = 24f;
        }

        bool Crossed => SisX > patchStart + patchLen;

        // Крутим непрерывно, «!» по флагу; возвращаемся, когда участок пройден, игрок упал
        // или вышло время.
        IEnumerator CrankUntilCrossed(float seconds, float degPerSecond, bool bangHeld)
        {
            float t = 0f;
            while (t < seconds && game != null && game.State == GState.Playing && !Crossed)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                t += dt;
                fake.Next = new BackendSnapshot
                {
                    CrankDeltaDegrees = degPerSecond * dt,
                    BangHeld = bangHeld
                };
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator BangHeld_WithCrank_CrossesSteepPatch()
        {
            PlaceSteepAhead();
            yield return CrankUntilCrossed(12f, 1200f, bangHeld: true);

            Assert.AreEqual(GState.Playing, game.State,
                "правильная комбинация «! + крутилка» не должна ронять игрока на крутом склоне (упал: "
                + game.FallReason + ", силы: " + game.Stamina.ToString("0.0") + ")");
            Assert.IsTrue(Crossed,
                "зажатый «!» + вращение обязаны преодолевать крутой участок (высота: "
                + game.Height.ToString("0.0") + ", силы: " + game.Stamina.ToString("0.0") + ")");
        }

        // Контроль: без «!» склон обязан РЕАЛЬНО жечь силы (штраф автора за неверную игру жив).
        // Мы намеренно НЕ утверждаем «без «!» пройти невозможно»: на высокой скорости крутилки
        // поток толчков продавливает участок и без «!» — это ПРЕЖНЕЕ свойство баланса (оно было
        // и до слияния, потолок SteepMaxMomentum держит скорость, а не запрещает проход),
        // и превращать его в баг без основательницы нельзя.
        [UnityTest]
        public IEnumerator NoBang_WithCrank_PaysSteepPenalty()
        {
            PlaceSteepAhead();
            float before = game.Stamina;
            yield return CrankUntilCrossed(12f, 1200f, bangHeld: false);

            Assert.Less(game.Stamina, before - GameConfig.DrainSteep * 2f,
                "подъём по склону без «!» обязан списывать силы штрафом DrainSteep (списано: "
                + (before - game.Stamina).ToString("0.0") + ")");
        }
    }
}
