using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AiGameStudio.ArcadeControls;

namespace EndlessSisyphus.Tests
{
    /// <summary>
    /// Имя цветной кнопки стойки — цветом этой кнопки, прямо внутри подсказки.
    ///
    /// Решение основательницы: «кнопки которые нужно нажимать выделить цветом в тексте».
    /// Игрок стоит у автомата, где кнопки физически жёлтая, зелёная и красная; слово
    /// «красную» в подсказке обязано быть красным, иначе связь «текст ↔ железка под рукой»
    /// игрок достраивает сам.
    ///
    /// Тесты держат три вещи, каждую из которых следующая правка потеряла бы молча:
    ///   • имя кнопки красится — и красится ЦЕЛИКОМ («красную кнопку», а не «красную»);
    ///   • красится в СВОЙ цвет, а не в какой попало;
    ///   • органы без цвета на стойке (крутилка, кнопка меню) не красятся вовсе — иначе
    ///     игра начнёт обещать код, которого на стойке нет.
    ///
    /// Отдельно стережём способ: цвет считается по тексту и применяется на отрисовке,
    /// поэтому сами строки остаются ровно теми, которые утвердила основательница —
    /// никакой разметки внутри констант.
    /// </summary>
    public class HintHighlightTests
    {
        GameObject host;
        SisyphusGame game;
        GameUI ui;
        FakeBackend fake;

        const float W = 1920f, H = 1080f;
        const float Scale = 1.5f;

        static float FakeMeasure(string text, int fontSize) => text.Length * fontSize * 0.52f;

        // Заведомо «никакой» базовый цвет: любое отличие от него — это подсветка.
        static readonly Color Base = new Color(1f, 0.95f, 0.82f);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            host = new GameObject("SisyphusGame_HintHighlight");
            ArcadeInput.Initialize(null);
            game = host.AddComponent<SisyphusGame>();
            ui = host.AddComponent<GameUI>();
            ui.game = game;
            fake = new FakeBackend();
            ArcadeInput.Initialize(fake);
            yield return null;

            game.StartGame();
            game.IntroActive = false;
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

        List<GameUI.HintPlate> Layout(float quoteTop = H) =>
            ui.LayoutHintPlates(W, H, Scale, FakeMeasure, quoteTop);

        // ── помощники ──────────────────────────────────────────────────────────────────

        /// <summary>Всё, что покрашено НЕ базовым цветом. Сравнение с ожидаемой строкой
        /// ловит сразу оба провала: подсветка пропала и подсветилось лишнее.</summary>
        static string Highlighted(string text, Color baseColor)
        {
            Color[] colors = GameUI.HintTextColors(text, baseColor);
            Assert.AreEqual(text.Length, colors.Length,
                "цвет обязан быть посчитан для каждого символа строки «" + text + "»");
            var painted = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
                if (colors[i] != baseColor) painted.Append(text[i]);
            return painted.ToString();
        }

        /// <summary>Кусок, покрашенный именно этим цветом; заодно проверяет, что кусок
        /// сплошной — подсветка вразбивку не выделяет имя органа, а рябит.</summary>
        static string PaintedWith(string text, Color baseColor, Color organ)
        {
            Color[] colors = GameUI.HintTextColors(text, baseColor);
            int first = -1, last = -1;
            for (int i = 0; i < colors.Length; i++)
                if (colors[i] == organ) { if (first < 0) first = i; last = i; }
            if (first < 0) return string.Empty;
            for (int i = first; i <= last; i++)
                Assert.AreEqual(organ, colors[i],
                    "подсветка имени органа обязана быть сплошной, а не вразбивку: «" + text + "»");
            return text.Substring(first, last - first + 1);
        }

        static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        // ── имя кнопки красится, и красится целиком ────────────────────────────────────

        [UnityTest]
        public IEnumerator SteepHint_PaintsTheYellowButtonYellow()
        {
            const string text = "Крутой склон. Зажми и держи жёлтую кнопку и продолжай крутить.";
            Assert.AreEqual("жёлтую кнопку", Highlighted(text, Base),
                "в подсказке крутого склона выделено имя жёлтой кнопки — и только оно");
            Assert.AreEqual("жёлтую кнопку", PaintedWith(text, Base, GameUI.OrganYellow),
                "жёлтая кнопка обязана краситься цветом жёлтой кнопки");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RainHints_PaintTheRedButtonRed()
        {
            string[] texts =
            {
                "Пошла морось. Включи защиту от дождя: нажми 1 раз красную кнопку и продолжай крутить.",
                "Пошёл дождь. Включи защиту от дождя: нажми 1 раз красную кнопку и продолжай крутить.",
                "Пошёл ливень. Включи защиту от дождя: нажми 1 раз красную кнопку и продолжай крутить.",
                "Дождя нет. Отключи защиту от дождя: нажми красную кнопку.",
                "Дождь прошёл. Отключи защиту от дождя: нажми красную кнопку."
            };
            foreach (string text in texts)
            {
                Assert.AreEqual("красную кнопку", Highlighted(text, Base), "в «" + text + "»");
                Assert.AreEqual("красную кнопку", PaintedWith(text, Base, GameUI.OrganRed),
                    "красная кнопка обязана краситься цветом красной кнопки: «" + text + "»");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ScreenButtons_PaintTheGreenButtonGreen()
        {
            foreach (string text in new[] { GameUI.StartButtonLabel, GameUI.RestartButtonLabel })
            {
                Assert.AreEqual("ЗЕЛЁНАЯ КНОПКА", Highlighted(text, Base),
                    "на кнопке экрана выделено имя зелёной кнопки — и только оно: «" + text + "»");
                Assert.AreEqual("ЗЕЛЁНАЯ КНОПКА", PaintedWith(text, Base, GameUI.OrganGreen),
                    "ВЕРХНИЙ регистр — это то же имя органа, и красится он так же: «" + text + "»");
            }
            yield return null;
        }

        // ── органы без цвета на стойке не красятся ─────────────────────────────────────

        [UnityTest]
        public IEnumerator OrgansWithoutAColor_StayInTheTextColor()
        {
            // У крутилки, датчиков и кнопки меню цвета на стойке нет. Покрасить их значило бы
            // выдумать игроку код, которого под рукой не существует.
            string[] texts =
            {
                GameUI.CrankStartHint,
                "КНОПКА МЕНЮ — выход",
                "ЗАЩИТА ОТ ДОЖДЯ — ВКЛ",
                "Камень наехал на лёд. Старайся крутить ровно.",
                "ВЕТЕР СТИХ — КРУТИ",
                "ТУРБУЛЕНТНЫЙ ВЕТЕР — НЕ КРУТИ"
            };
            foreach (string text in texts)
                Assert.AreEqual(string.Empty, Highlighted(text, Base),
                    "здесь красить нечего — цветных кнопок в строке нет: «" + text + "»");
            yield return null;
        }

        // ── подсветка доживает до отрисовки через всю раскладку ────────────────────────

        [UnityTest]
        public IEnumerator EveryPlateNamingAColoredButton_CarriesTheHighlight()
        {
            var seen = new List<GameUI.HintPlate>();

            for (int variant = 0; variant < 3; variant++)
            {
                PinCalm();
                game.Obstacle = ObKind.Rain; game.Phase = ObPhase.Active;
                game.RainVariant = variant;
                seen.AddRange(Layout());
            }
            PinCalm();
            game.Obstacle = ObKind.Steep; game.Phase = ObPhase.Active;
            game.SteepActive = game.IsOnSteep = true;
            game.SteepStart = game.Scroll; game.SteepLen = 400f;
            seen.AddRange(Layout());
            PinCalm();
            game.Careful = true; game.CarefulBad = true; seen.AddRange(Layout());
            game.CarefulBad = false; game.RainExitGrace = 2f; seen.AddRange(Layout());
            game.RainExitGrace = 0f; seen.AddRange(Layout());
            PinCalm();
            seen.AddRange(Layout());

            Assert.Greater(seen.Count, 6, "предусловие: собрали подсказки всех состояний");

            int highlighted = 0;
            foreach (var plate in seen)
            {
                Assert.IsNotNull(plate.LineColors,
                    "раскладка обязана отдавать цвета символов — иначе рисовать подсветку нечем");
                Assert.AreEqual(plate.Lines.Length, plate.LineColors.Length,
                    "цвета обязаны быть у каждой строки плашки «" + plate.Id + "»");
                for (int i = 0; i < plate.Lines.Length; i++)
                    Assert.AreEqual(plate.Lines[i].Length, plate.LineColors[i].Length,
                        "цвета обязаны быть у каждого символа строки плашки «" + plate.Id + "»");

                // Слитый обратно текст красится ровно так же, как он покрашен по строкам:
                // перенос не имеет права съесть подсветку.
                Color[] whole = GameUI.HintTextColors(plate.Text, plate.TextColor);
                int at = 0;
                for (int i = 0; i < plate.Lines.Length; i++)
                {
                    at = plate.Text.IndexOf(plate.Lines[i], at, System.StringComparison.Ordinal);
                    Assert.GreaterOrEqual(at, 0, "строки плашки обязаны быть кусками её текста");
                    for (int c = 0; c < plate.Lines[i].Length; c++)
                        Assert.AreEqual(whole[at + c], plate.LineColors[i][c],
                            "перенос строки потерял подсветку в плашке «" + plate.Id + "»: «" +
                            plate.Text + "»");
                    at += plate.Lines[i].Length;
                }

                if (plate.Text.ToLowerInvariant().Contains("кнопк"))
                {
                    Assert.AreNotEqual(string.Empty, Highlighted(plate.Text, plate.TextColor),
                        "подсказка называет кнопку, но имя кнопки не покрашено: «" + plate.Text + "»");
                    highlighted++;
                }
            }
            Assert.Greater(highlighted, 3,
                "предусловие: среди собранных подсказок есть называющие цветные кнопки");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Highlight_SurvivesTheLineBreak()
        {
            // Самая длинная подсказка игры на узком экране переносится, и имя кнопки вполне
            // может разъехаться по двум строкам. Красится оно всё равно целиком.
            PinCalm();
            game.Obstacle = ObKind.Rain; game.Phase = ObPhase.Active; game.RainVariant = 2;
            var plates = ui.LayoutHintPlates(760f, 600f, 0.78f, FakeMeasure, 600f);
            var banner = plates.Find(p => p.Id == "banner");
            Assert.IsNotNull(banner.Id, "предусловие: баннер дождя на экране");
            Assert.Greater(banner.Lines.Length, 1, "предусловие: на узком экране подсказка перенеслась");

            var painted = new StringBuilder();
            for (int i = 0; i < banner.Lines.Length; i++)
                for (int c = 0; c < banner.Lines[i].Length; c++)
                    if (banner.LineColors[i][c] == GameUI.OrganRed) painted.Append(banner.Lines[i][c]);
            Assert.AreEqual("краснуюкнопку", painted.ToString().Replace(" ", string.Empty),
                "имя красной кнопки обязано остаться красным целиком, даже если перенос " +
                "разорвал его между строками");
            yield return null;
        }

        // ── сами цвета ─────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator OrganColors_AreReadableOnThePlate_AndUnmistakable()
        {
            // Читаемость. Подложка плашки — почти чёрная (HintPlateFill), и тёмный цвет на ней
            // превращается в грязное пятно. Ровно за это основательница забраковала красный
            // бейдж защиты, и ровно поэтому красный подсветки — осветлённый.
            Assert.Less(Luminance(GameUI.HintPlateFill), 0.1f, "предусловие: подложка тёмная");
            foreach (var pair in new[]
                     {
                         new KeyValuePair<string, Color>("жёлтый", GameUI.OrganYellow),
                         new KeyValuePair<string, Color>("зелёный", GameUI.OrganGreen),
                         new KeyValuePair<string, Color>("красный", GameUI.OrganRed)
                     })
                Assert.Greater(Luminance(pair.Value), 0.45f,
                    "цвет «" + pair.Key + "» обязан быть живым на тёмной подложке");
            Assert.Greater(Luminance(GameUI.OrganRed), Luminance(Color.red) * 2f,
                "чистый красный на подложке не читается — красный подсветки обязан быть " +
                "заметно светлее него");

            // Опознаваемость: каждый цвет обязан читаться как свой, а не «какой-то тёплый».
            Assert.Greater(GameUI.OrganYellow.r, 0.85f, "жёлтый");
            Assert.Greater(GameUI.OrganYellow.g, 0.75f, "жёлтый");
            Assert.Less(GameUI.OrganYellow.b, 0.4f, "жёлтый не имеет права уходить в белый");

            Assert.Greater(GameUI.OrganGreen.g - GameUI.OrganGreen.r, 0.25f, "зелёный");
            Assert.Greater(GameUI.OrganGreen.g - GameUI.OrganGreen.b, 0.25f, "зелёный");

            Assert.Greater(GameUI.OrganRed.r - GameUI.OrganRed.g, 0.4f, "красный");
            Assert.Greater(GameUI.OrganRed.r - GameUI.OrganRed.b, 0.4f, "красный");

            // Между собой и с кодами игры цвета не должны слипаться.
            Assert.AreNotEqual(GameUI.OrganYellow, GameUI.OrganGreen);
            Assert.AreNotEqual(GameUI.OrganGreen, GameUI.OrganRed);
            Assert.AreNotEqual(GameUI.OrganYellow, GameUI.OrganRed);
            Assert.AreNotEqual(GameUI.OrganYellow, GameUI.HintPlateCrankText,
                "золото игры означает «от тебя нужно действие»; имя жёлтой кнопки — другой " +
                "знак, и совпадать они не должны");
            Assert.Greater(Mathf.Abs(GameUI.OrganGreen.b - GameUI.RainGuardColor(false).b), 0.2f,
                "зелёная кнопка не должна путаться с бирюзой бейджа «всё правильно»");
            yield return null;
        }

        // ── способ: константы текста не тронуты ────────────────────────────────────────

        [UnityTest]
        public IEnumerator HintTexts_CarryNoMarkup()
        {
            // Подсветка живёт на отрисовке. Если её когда-нибудь перенесут в richText-разметку
            // внутри строк, тексты перестанут быть теми, что утвердила основательница, поедет
            // посимвольный замер плашки, а теги вылезут на экран буквами.
            PinCalm();
            game.Obstacle = ObKind.Rain; game.Phase = ObPhase.Active; game.RainVariant = 2;
            var texts = new List<string> { GameUI.CrankStartHint, GameUI.Epigraph,
                GameUI.StartButtonLabel, GameUI.RestartButtonLabel };
            foreach (var plate in Layout()) texts.Add(plate.Text);
            PinCalm();
            game.Careful = true; game.CarefulBad = true;
            foreach (var plate in Layout()) texts.Add(plate.Text);

            foreach (string text in texts)
            {
                StringAssert.DoesNotContain("<color", text, "разметки в тексте игры быть не должно");
                StringAssert.DoesNotContain("</", text, "разметки в тексте игры быть не должно");
            }

            Assert.AreEqual("Крути крутилку, чтобы толкать камень.", GameUI.CrankStartHint);
            Assert.AreEqual("ЗЕЛЁНАЯ КНОПКА — НАЧАТЬ", GameUI.StartButtonLabel);
            Assert.AreEqual("ЗЕЛЁНАЯ КНОПКА — ЗАНОВО", GameUI.RestartButtonLabel);
            yield return null;
        }
    }
}
