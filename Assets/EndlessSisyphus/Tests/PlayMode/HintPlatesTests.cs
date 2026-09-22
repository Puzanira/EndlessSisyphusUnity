using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AiGameStudio.ArcadeControls;

namespace EndlessSisyphus.Tests
{
    /// <summary>
    /// Единая плашка подсказок геймплея.
    ///
    /// Решение основательницы у живого автомата: «все подсказки к геймплею надо сделать на
    /// одинаковых плашках». До этого подсказок было три вида — подсказка первого действия на
    /// тёмной плашке с золотой рамкой, баннеры препятствий тонкой строкой по пейзажу и бейдж
    /// защиты от дождя тоже без плашки. Каждый вид рисовался своим помощником, и именно
    /// поэтому они разъехались.
    ///
    /// Тесты стерегут ровно это: ВСЕ подсказки геймплея выкладываются одним
    /// <see cref="GameUI.LayoutHintPlates"/> с одной геометрией, старые пер-подсказочные
    /// рисовальщики не воскресают, а выросшие плашки не наезжают ни друг на друга, ни на HUD.
    ///
    /// Цитаты Камю и эпиграф сюда НЕ входят: у них свой каменный слой (DrawStoneQuote), это
    /// атмосфера, а не подсказка.
    /// </summary>
    public class HintPlatesTests
    {
        GameObject host;
        SisyphusGame game;
        GameUI ui;
        FakeBackend fake;

        const float W = 1920f, H = 1080f;
        // UiScale на кадре стойки 1920x1080: min(1920/1280, 1080/720) = 1.5.
        const float Scale = 1.5f;

        // Замер вынесен в делегат именно затем, чтобы раскладку можно было проверить без
        // OnGUI: шрифты в тестовом прогоне мерить нечем и незачем. Метрика пропорциональная —
        // ширина растёт и от длины строки, и от кегля, как у настоящей.
        static float FakeMeasure(string text, int fontSize) => text.Length * fontSize * 0.52f;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            host = new GameObject("SisyphusGame_HintPlates");
            ArcadeInput.Initialize(null);
            game = host.AddComponent<SisyphusGame>();
            ui = host.AddComponent<GameUI>();
            ui.game = game;
            fake = new FakeBackend();
            ArcadeInput.Initialize(fake);
            yield return null;

            game.StartGame();
            game.IntroActive = false;           // подсказка первого действия живёт после вступления
            game.IntroT = GameConfig.IntroDuration;
            yield return null;
            PinCalm();
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
            game.WindExitGrace = 0f;
            game.IceActive = game.IsOnIce = false;
            game.SteepActive = game.IsOnSteep = false;
            game.Careful = false;
        }

        void PinRain(int variant)
        {
            PinCalm();
            game.Obstacle = ObKind.Rain;
            game.Phase = ObPhase.Active;
            game.RainVariant = variant;
        }

        List<GameUI.HintPlate> Layout(float quoteTop = H) =>
            ui.LayoutHintPlates(W, H, Scale, FakeMeasure, quoteTop);

        GameUI.HintPlate Single(string id, List<GameUI.HintPlate> plates)
        {
            var found = plates.Find(p => p.Id == id);
            Assert.IsNotNull(found.Id, "ожидалась плашка «" + id + "», а на экране: " +
                string.Join(", ", plates.ConvertAll(p => p.Id).ToArray()));
            return found;
        }

        /// <summary>Та самая геометрия эталона — плашки первого действия: отступы, высота
        /// строки и рамка считаются из общих формул, а не зашиты в каждую подсказку.</summary>
        static void AssertPlateGeometry(GameUI.HintPlate plate)
        {
            float padX = GameUI.HintPlatePadX(Scale), padY = GameUI.HintPlatePadY(Scale);
            float lineHeight = GameUI.HintPlateLineHeight(plate.FontSize);

            Assert.AreEqual(plate.Lines.Length * lineHeight + padY * 2f, plate.Box.height, 0.01f,
                "высота плашки «" + plate.Id + "» обязана складываться из общих отступов и " +
                "общей высоты строки — иначе плашки перестанут быть одинаковыми");

            float widest = 0f;
            for (int i = 0; i < plate.Lines.Length; i++)
                widest = Mathf.Max(widest, FakeMeasure(plate.Lines[i], plate.FontSize));
            Assert.AreEqual(Mathf.Min(widest + padX * 2f, W * 0.92f), plate.Box.width, 0.01f,
                "ширина плашки «" + plate.Id + "» обязана считаться по строке теми же " +
                "отступами: подсказки основательницы — цельные предложения, и жёсткая " +
                "рамка рано или поздно режет очередную формулировку");

            Assert.GreaterOrEqual(plate.Box.xMin, 0f, "плашка «" + plate.Id + "» уехала за левый край");
            Assert.LessOrEqual(plate.Box.xMax, W, "плашка «" + plate.Id + "» уехала за правый край");
            Assert.GreaterOrEqual(plate.Box.yMin, 0f, "плашка «" + plate.Id + "» уехала за верхний край");
            Assert.LessOrEqual(plate.Box.yMax, H, "плашка «" + plate.Id + "» уехала за нижний край");
        }

        static void AssertClearOfHud(GameUI.HintPlate plate)
        {
            foreach (var zone in GameUI.HudReservedZones(W, Scale))
                Assert.IsFalse(plate.Box.Overlaps(zone),
                    "плашка «" + plate.Id + "» " + plate.Box + " наехала на служебную зону HUD " +
                    zone + " (шкала СИЛ / высота / рекорд)");
        }

        static void AssertNoMutualOverlap(List<GameUI.HintPlate> plates)
        {
            for (int i = 0; i < plates.Count; i++)
                for (int j = i + 1; j < plates.Count; j++)
                    Assert.IsFalse(plates[i].Box.Overlaps(plates[j].Box),
                        "плашки «" + plates[i].Id + "» " + plates[i].Box + " и «" +
                        plates[j].Id + "» " + plates[j].Box + " перекрылись: с подложками " +
                        "подсказки стали крупнее, и разводить их по экрану теперь обязательно");
        }

        static void AssertHealthy(List<GameUI.HintPlate> plates)
        {
            foreach (var plate in plates) { AssertPlateGeometry(plate); AssertClearOfHud(plate); }
            AssertNoMutualOverlap(plates);
        }

        // ── единство вида ──────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator EveryGameplayHint_ComesFromTheOneLayout()
        {
            // баннер препятствия
            PinRain(2);
            Assert.IsNotNull(Single("banner", Layout()).Id);

            // подсказка первого действия
            PinCalm();
            Assert.IsTrue(ui.CrankHintVisible, "предусловие: игрок ещё не крутил");
            Assert.AreEqual(GameUI.CrankStartHint, Single("crank", Layout()).Text,
                "подсказка первого действия обязана идти тем же помощником, что и остальные");

            // бейдж защиты от дождя
            PinCalm();
            game.Careful = true;
            Assert.IsNotNull(Single("guard", Layout()).Id);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllHints_ShareOnePlateGeometry_AcrossEveryState()
        {
            var seen = new List<GameUI.HintPlate>();

            for (int variant = 0; variant < 3; variant++) { PinRain(variant); seen.AddRange(Layout()); }
            game.Obstacle = ObKind.Ice; game.Phase = ObPhase.Active;
            game.IceActive = game.IsOnIce = true;
            game.IceStart = game.Scroll; game.IceLen = 400f;
            seen.AddRange(Layout());
            PinCalm();
            game.Obstacle = ObKind.Steep; game.Phase = ObPhase.Active;
            game.SteepActive = game.IsOnSteep = true;
            game.SteepStart = game.Scroll; game.SteepLen = 400f;
            seen.AddRange(Layout());
            PinCalm();
            game.Obstacle = ObKind.Wind; game.Phase = ObPhase.Active;
            for (int variant = 0; variant < 3; variant++) { game.WindVariant = variant; seen.AddRange(Layout()); }
            PinCalm();
            game.WindExitGrace = 1f; seen.AddRange(Layout());
            PinCalm();
            seen.AddRange(Layout());                                   // подсказка первого действия
            game.Careful = true; game.CarefulBad = true; seen.AddRange(Layout());
            game.CarefulBad = false; game.RainExitGrace = 2f; seen.AddRange(Layout());
            game.RainExitGrace = 0f; seen.AddRange(Layout());          // «ЗАЩИТА ОТ ДОЖДЯ — ВКЛ»

            Assert.Greater(seen.Count, 12, "предусловие: собрали все состояния подсказок");
            foreach (var plate in seen) { AssertPlateGeometry(plate); AssertClearOfHud(plate); }

            // Один набор: отступы и высота строки у всех выводятся из общих формул, значит
            // при равном кегле высота однострочной плашки обязана совпадать до пикселя.
            var byFont = new Dictionary<int, float>();
            foreach (var plate in seen)
            {
                if (plate.Lines.Length != 1) continue;
                if (byFont.TryGetValue(plate.FontSize, out float h))
                    Assert.AreEqual(h, plate.Box.height, 0.01f,
                        "однострочные плашки одного кегля обязаны быть одной высоты — " +
                        "это и есть «одинаковые плашки»");
                else byFont[plate.FontSize] = plate.Box.height;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator LegacyPerHintDrawers_AreGone()
        {
            const BindingFlags any = BindingFlags.Instance | BindingFlags.Static |
                                     BindingFlags.Public | BindingFlags.NonPublic;
            Assert.IsNull(typeof(GameUI).GetMethod("DrawCrankStartHint", any),
                "подсказка первого действия больше не рисуется своим помощником — " +
                "она идёт общей плашкой (LayoutHintPlates + PaintHintPlate)");
            Assert.IsNull(typeof(GameUI).GetMethod("DrawFittedBanner", any),
                "баннеры препятствий больше не рисуются своим помощником — " +
                "подгонка ширины переехала внутрь общей плашки");
            Assert.IsNotNull(typeof(GameUI).GetMethod("PaintHintPlate", any),
                "оформление плашки обязано жить в одном месте");
            yield return null;
        }

        // ── цвета ──────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator PlateColors_KeepTheMeaningOfGold_AndTurquoise()
        {
            PinCalm();
            Assert.AreEqual(GameUI.HintPlateCrankText, Single("crank", Layout()).TextColor,
                "подсказка первого действия остаётся золотом игры");

            PinCalm();
            game.Careful = true; game.CarefulBad = true;
            Assert.AreEqual(GameUI.RainGuardColor(true), Single("guard", Layout()).TextColor,
                "«Дождя нет. Отключи защиту…» — золото: от игрока нужно действие");

            game.CarefulBad = false; game.RainExitGrace = 0f;
            var guardOn = Single("guard", Layout());
            Assert.AreEqual(GameUI.RainGuardColor(false), guardOn.TextColor,
                "«ЗАЩИТА ОТ ДОЖДЯ — ВКЛ» остаётся бирюзовой: всё правильно, действия не нужно");
            Assert.AreNotEqual(GameUI.RainGuardColor(true), guardOn.TextColor,
                "различение «нужно действие» / «всё правильно» плашка терять не должна");

            PinRain(1);
            Assert.AreEqual(GameUI.HintPlateBannerText, Single("banner", Layout()).TextColor,
                "баннер препятствия остаётся узнаваемо не-золотым");
            yield return null;
        }

        // ── длинные строки ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator LongestHint_Wraps_InsteadOfBeingCut()
        {
            PinRain(2);                                   // «Пошёл ливень. Включи защиту…»
            var plate = Single("banner", Layout());
            StringAssert.Contains("Пошёл ливень.", plate.Text);
            StringAssert.Contains("продолжай крутить.", plate.Text);

            string joined = string.Join(" ", plate.Lines);
            Assert.AreEqual(plate.Text, joined,
                "самая длинная строка игры обязана переноситься целиком, а не обрезаться");
            AssertPlateGeometry(plate);

            // Та же строка на узком экране: строк становится больше, слова — те же.
            var narrow = ui.LayoutHintPlates(900f, 600f, 0.78f, FakeMeasure, 600f);
            var narrowPlate = narrow.Find(p => p.Id == "banner");
            Assert.AreEqual(plate.Text, string.Join(" ", narrowPlate.Lines),
                "на узком экране подсказка тоже переносится, а не теряет хвост");
            Assert.LessOrEqual(narrowPlate.Box.width, 900f * 0.92f + 0.01f,
                "плашка не имеет права вылезти за потолок 92 % экрана");
            yield return null;
        }

        // ── пересечения ────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator HintAndBanner_NeverShareTheScreen()
        {
            // Подсказка первого действия и баннер препятствия занимают одну роль —
            // «что делать сейчас». Баннер имеет приоритет по построению; тест закрепляет,
            // что накладываться им попросту негде.
            for (int variant = 0; variant < 3; variant++)
            {
                PinRain(variant);
                Assert.IsTrue(game.AwaitingFirstPush, "предусловие: игрок ещё не крутил");
                var plates = Layout();
                Assert.IsNull(plates.Find(p => p.Id == "crank").Id,
                    "пока висит баннер препятствия, подсказка первого действия не показывается");
                AssertHealthy(plates);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator BannerAndGuard_Coexist_WithoutTouching()
        {
            // Реальный кадр: дождь идёт, игрок уже нажал красную — сверху баннер дождя,
            // снизу бейдж «ЗАЩИТА ОТ ДОЖДЯ — ВКЛ».
            for (int variant = 0; variant < 3; variant++)
            {
                PinRain(variant);
                game.Careful = true; game.CarefulBad = false; game.RainExitGrace = 0f;
                var plates = Layout();
                Assert.AreEqual(2, plates.Count, "на экране обязаны быть обе подсказки");
                Assert.IsNotNull(Single("banner", plates).Id);
                Assert.IsNotNull(Single("guard", plates).Id);
                AssertHealthy(plates);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator HintAndGuard_Coexist_WithoutTouching()
        {
            PinCalm();
            game.Careful = true; game.CarefulBad = true;       // «Дождя нет. Отключи защиту…»
            var plates = Layout();
            Assert.AreEqual(2, plates.Count, "подсказка первого действия и бейдж защиты живут вместе");
            AssertHealthy(plates);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GuardPlate_StaysAboveTheCamusQuote()
        {
            // Каменная плашка цитаты Камю — не подсказка, но занимает низ экрана; бейдж
            // защиты обязан уехать выше неё, а не лечь поверх.
            const float quoteTop = 820f;
            PinRain(2);
            game.Careful = true; game.CarefulBad = false;
            var plates = Layout(quoteTop);
            var guard = Single("guard", plates);
            Assert.LessOrEqual(guard.Box.yMax, quoteTop,
                "бейдж защиты обязан стоять НАД каменной плашкой цитаты, а не наезжать на неё");
            AssertHealthy(plates);
            yield return null;
        }

        // ── тексты ─────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator HintWording_IsUntouched()
        {
            Assert.AreEqual("Крути крутилку, чтобы толкать камень.", GameUI.CrankStartHint);

            PinRain(2);
            Assert.AreEqual(
                "Пошёл ливень. Включи защиту от дождя: нажми 1 раз красную кнопку и продолжай крутить.",
                Single("banner", Layout()).Text);

            PinCalm();
            game.Careful = true; game.CarefulBad = true;
            Assert.AreEqual("Дождя нет. Отключи защиту от дождя: нажми красную кнопку.",
                Single("guard", Layout()).Text);

            game.CarefulBad = false; game.RainExitGrace = 2f;
            Assert.AreEqual("Дождь прошёл. Отключи защиту от дождя: нажми красную кнопку.",
                Single("guard", Layout()).Text);

            game.RainExitGrace = 0f;
            Assert.AreEqual("ЗАЩИТА ОТ ДОЖДЯ — ВКЛ", Single("guard", Layout()).Text);
            yield return null;
        }
    }
}
