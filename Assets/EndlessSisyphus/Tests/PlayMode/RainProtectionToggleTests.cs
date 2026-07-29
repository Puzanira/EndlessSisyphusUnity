using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AiGameStudio.ArcadeControls;

namespace EndlessSisyphus.Tests
{
    /// <summary>
    /// Конец-в-конец проверка защитного режима дождя на живом SisyphusGame + FakeBackend
    /// (репро жалобы основательницы «нажимаю red, кручу — падаю»):
    ///  - один press красной во время дождя + непрерывная крутилка → НЕ падает, slip подавлен;
    ///  - ЗАЖАТАЯ красная не «мерцает» тогглом (гард от дребезга/автоповтора);
    ///  - крутилка с паузами между щелчками (0.2s < hold-bridge 0.25s) не считается
    ///    «прекратил вращение» — SpaceDown жив, дождь пережит;
    ///  - повторный press после дождя снимает режим;
    ///  - контроль-репро: дождь БЕЗ красной → slip-падение (симптом, когда red не доходит).
    /// </summary>
    public class RainProtectionToggleTests
    {
        GameObject host;
        SisyphusGame game;
        FakeBackend fake;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            host = new GameObject("SisyphusGame_UnderTest");
            game = host.AddComponent<SisyphusGame>();   // Awake: мир, звук, ArcadeControlsAdapter.Initialize
            fake = new FakeBackend();
            ArcadeInput.Initialize(fake);               // подменяем клавиатурный бэкенд фейком
            yield return null;                          // прожить Awake/первый Update

            game.StartGame();
            game.IntroActive = false;                   // пропустить вступительную анимацию
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

        // Дождь держим активным принудительно (пиним поля каждый кадр — обходим таймеры спавна).
        void PinRainActive()
        {
            game.Obstacle = ObKind.Rain;
            game.Phase = ObPhase.Active;
            game.ObTimer = 999f;
        }

        void PinCalm()
        {
            game.Obstacle = ObKind.None;
            game.Phase = ObPhase.Calm;
            game.ObTimer = 999f;
        }

        // Прогнать seconds игрового времени; каждый кадр держит дождь и крутит крутилку.
        // crankPeriod: 0 = непрерывное вращение (каждый кадр), иначе один «щелчок» раз в period сек.
        IEnumerator RunRain(float seconds, float degPerSecond, float crankPeriod, bool redHeld)
        {
            float t = 0f, sinceNotch = 0f;
            while (t < seconds && game != null && game.State == GState.Playing)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                t += dt; sinceNotch += dt;
                PinRainActive();
                float delta = 0f;
                if (crankPeriod <= 0f) delta = degPerSecond * dt;
                else if (sinceNotch >= crankPeriod) { delta = degPerSecond * crankPeriod; sinceNotch = 0f; }
                fake.Next = new BackendSnapshot { CrankDeltaDegrees = delta, RedHeld = redHeld };
                yield return null;
            }
        }

        IEnumerator PressRedOnce()
        {
            fake.Next = new BackendSnapshot { RedHeld = true };
            yield return null;                          // фронт press
            fake.Next = new BackendSnapshot { RedHeld = false };
            yield return null;                          // отпустили
        }

        [UnityTest]
        public IEnumerator RedPress_DuringRain_EnablesProtection_NoFall()
        {
            PinRainActive();
            yield return PressRedOnce();
            Assert.IsTrue(game.Careful, "один press красной должен включить защитный режим");

            // 4с активного дождя с непрерывной крутилкой — не падаем, slip подавлен
            yield return RunRain(4f, 120f, 0f, redHeld: false);
            Assert.AreEqual(GState.Playing, game.State,
                "с включённой защитой и вращением игрок не должен падать в дождь (упал: " + game.FallReason + ")");
            Assert.Less(game.SlipRisk, 0.5f, "slip-риск должен подавляться защитным режимом");
            Assert.IsTrue(game.Careful, "режим не должен слетать сам по себе во время дождя");
        }

        [UnityTest]
        public IEnumerator HeldRed_DoesNotFlickerToggle()
        {
            PinRainActive();
            // красная ЗАЖАТА 2с (эмуляция «нажимаю и держу» / автоповтора ОС):
            // edge-детект обязан дать ровно один toggle — режим стабильно ВКЛ
            yield return RunRain(2f, 120f, 0f, redHeld: true);
            Assert.AreEqual(GState.Playing, game.State, "зажатая красная не должна ронять игрока");
            Assert.IsTrue(game.Careful,
                "зажатая красная = один press-фронт: режим должен остаться ВКЛ (мерцание тоггла = баг)");
        }

        [UnityTest]
        public IEnumerator CrankNotchGaps_WithinHoldBridge_SurviveRain()
        {
            PinRainActive();
            yield return PressRedOnce();
            Assert.IsTrue(game.Careful);

            // щелчок колеса раз в 0.2с (пауза < CrankHoldSeconds 0.25с): мост обязан держать
            // SpaceDown, а игрок — переживать дождь, продолжая «крутить» с паузами
            yield return RunRain(4f, 120f, 0.2f, redHeld: false);
            Assert.AreEqual(GState.Playing, game.State,
                "щелчковое вращение (гэп 0.2с < бриджа 0.25с) не должно считаться остановкой (упал: "
                + game.FallReason + ")");
            Assert.IsTrue(game.SpaceDown, "hold-bridge должен держать 'толкает сейчас' между щелчками");
        }

        [UnityTest]
        public IEnumerator RedPress_AfterRain_DisablesProtection()
        {
            PinRainActive();
            yield return PressRedOnce();
            Assert.IsTrue(game.Careful);
            yield return RunRain(1f, 120f, 0f, redHeld: false);

            // дождь кончился; ждём спада интенсивности, потом повторный press
            float t = 0f;
            while (t < 2f) { PinCalm(); fake.Next = new BackendSnapshot(); t += Time.deltaTime; yield return null; }
            yield return PressRedOnce();
            Assert.IsFalse(game.Careful, "повторный press красной после дождя должен ВЫключить режим");
        }

        [UnityTest]
        public IEnumerator NoRed_DuringRain_FallsBySlip()
        {
            // контроль-репро симптома основательницы: red не доходит (не та клавиша) →
            // защита не включена → slip-падение за считанные секунды
            yield return RunRain(6f, 120f, 0f, redHeld: false);
            Assert.AreEqual(GState.Falling, game.State, "без защиты дождь обязан ронять");
            Assert.AreEqual("slip", game.FallReason, "падение должно идти по slip-ветке дождя");
        }
    }
}
