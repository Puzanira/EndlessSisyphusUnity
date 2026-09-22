using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AiGameStudio.ArcadeControls;

namespace EndlessSisyphus.Tests
{
    /// <summary>
    /// Начало забега: эпиграф во вступлении и подсказка первого действия сразу после него.
    ///
    /// Подсказка (живой плейтест на стойке: «есть момент проигрыша, когда делать ничего не
    /// надо; после него вывести подсказку — крутить крутилку»). Весь проигрыш — падение,
    /// скатывание камня, экран проигрыша и вступление нового забега — проходит БЕЗ игрока:
    /// крутилка в это время мертва (canPush в HandleInput). Тесты закрепляют момент
    /// появления и момент снятия подсказки:
    ///  - пока крутилка мертва (вступление) — подсказки нет, она бы врала;
    ///  - как только крутилка ожила — подсказка есть;
    ///  - как только игрок реально крутнул — подсказка уходит;
    ///  - и всё это повторяется после поражения, а не только на первом забеге.
    ///
    /// Эпиграф живёт ровно в противофазе: он виден во вступлении, пока Сизиф идёт к камню,
    /// и обязан погаснуть к моменту, когда загорается подсказка. Тесты стерегут и это —
    /// два текста в начале забега не должны наложиться ни одним кадром.
    /// </summary>
    public class CrankStartHintTests
    {
        GameObject host;
        SisyphusGame game;
        GameUI ui;
        FakeBackend fake;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            host = new GameObject("SisyphusGame_CrankHint");
            // Сброс бэкенда ДО Awake — см. комментарий в SpeedCapTests.
            ArcadeInput.Initialize(null);
            game = host.AddComponent<SisyphusGame>();
            ui = host.AddComponent<GameUI>();
            ui.game = game;
            fake = new FakeBackend();
            ArcadeInput.Initialize(fake);
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

        // Спокойный участок: баннер препятствия имеет приоритет над подсказкой,
        // и без этого тест ловил бы случайное препятствие, а не саму подсказку.
        void PinCalm()
        {
            game.Obstacle = ObKind.None;
            game.Phase = ObPhase.Calm;
            game.ObTimer = 999f;
            game.WindExitGrace = 0f;
            game.IceActive = game.IsOnIce = false;
            game.SteepActive = game.IsOnSteep = false;
        }

        IEnumerator EndIntro()
        {
            // Вступление доигрывается само, без единого касания крутилки. Заодно каждый
            // кадр вступления проверяем, что подсказка и эпиграф не горят одновременно.
            float guard = 0f;
            while (game.IntroActive && guard < 12f)
            {
                guard += Mathf.Min(Time.deltaTime, 0.05f);
                fake.Next = new BackendSnapshot();
                PinCalm();
                AssertNoOverlap();
                yield return null;
            }
            PinCalm();
            yield return null;
        }

        void AssertNoOverlap() => Assert.IsFalse(ui.EpigraphVisible && ui.CrankHintVisible,
            "эпиграф и подсказка первого действия живут в начале забега — они не должны " +
            "оказаться на экране одновременно (IntroProgress " + game.IntroProgress + ")");

        [UnityTest]
        public IEnumerator Hint_Hidden_WhileIntroIgnoresTheCrank()
        {
            game.StartGame();
            yield return null;
            PinCalm();
            yield return null;

            Assert.IsTrue(game.IntroActive, "предусловие: забег начинается со вступления");
            Assert.IsFalse(ui.CrankHintVisible,
                "во вступлении крутилка ещё мертва — подсказка «крути» там была бы ложью");
        }

        [UnityTest]
        public IEnumerator Hint_Appears_WhenCrankGoesLive_AndLeaves_OnFirstPush()
        {
            game.StartGame();
            yield return EndIntro();

            Assert.IsFalse(game.IntroActive, "предусловие: вступление кончилось");
            Assert.IsTrue(game.AwaitingFirstPush, "предусловие: игрок ещё не крутил");
            Assert.IsTrue(ui.CrankHintVisible,
                "подсказка обязана появиться ровно тогда, когда от игрока снова нужно действие");
            Assert.AreEqual("Крути крутилку, чтобы толкать камень.", GameUI.CrankStartHint,
                "подсказка называет орган каноническим именем и связывает его с последствием");
            Assert.IsFalse(ui.EpigraphVisible,
                "к моменту, когда игрок берётся за крутилку, эпиграф обязан быть погашен");

            // Игрок крутит — подсказка обязана уйти.
            float guard = 0f;
            while (game.AwaitingFirstPush && guard < 3f)
            {
                guard += Mathf.Min(Time.deltaTime, 0.05f);
                PinCalm();
                fake.Next = new BackendSnapshot
                {
                    CrankDeltaDegrees = GameConfig.CrankDegreesPerPush * 2f
                };
                yield return null;
            }

            Assert.IsFalse(game.AwaitingFirstPush, "толчок крутилки обязан быть замечен");
            PinCalm();
            yield return null;
            Assert.IsFalse(ui.CrankHintVisible, "после первого толчка подсказка должна уйти");
        }

        [UnityTest]
        public IEnumerator Epigraph_BurnsThroughIntro_AndYieldsToTheHint()
        {
            Assert.IsFalse(ui.EpigraphVisible,
                "предусловие: на стартовом экране эпиграфа нет — он снят оттуда и переехал в забег");

            game.StartGame();

            // Дать вступлению разойтись: эпиграф проявляется, а не вспыхивает в первом кадре.
            float guard = 0f;
            while (game.IntroActive && game.IntroProgress < 0.5f && guard < 12f)
            {
                guard += Mathf.Min(Time.deltaTime, 0.05f);
                fake.Next = new BackendSnapshot();
                PinCalm();
                AssertNoOverlap();
                yield return null;
            }

            Assert.IsTrue(game.IntroActive, "предусловие: вступление ещё идёт");
            Assert.AreEqual(
                "«Боги приговорили Сизифа вечно вкатывать на вершину горы камень, который, едва достигнув цели, скатывался вниз»",
                GameUI.Epigraph, "эпиграф — дословный текст основательницы");
            Assert.IsTrue(ui.EpigraphVisible,
                "пока Сизиф идёт к камню, эпиграф обязан быть на экране");
            Assert.Greater(ui.EpigraphOpacity, 0.5f,
                "в середине вступления эпиграф читается в полную силу, а не еле проступает");
            Assert.IsFalse(ui.CrankHintVisible,
                "во вступлении крутилка мертва — подсказки «крути» там нет");

            yield return EndIntro();

            Assert.IsFalse(game.IntroActive, "предусловие: вступление кончилось");
            Assert.IsFalse(ui.EpigraphVisible,
                "с концом вступления эпиграф обязан уйти — дальше начинается игра");
            Assert.AreEqual(0f, ui.EpigraphOpacity, 0.0001f,
                "погашенный эпиграф не должен подсвечивать каменную плашку остаточной прозрачностью");
            Assert.IsTrue(ui.CrankHintVisible, "его место занимает подсказка первого действия");
        }

        [UnityTest]
        public IEnumerator Hint_ComesBack_AfterDefeat()
        {
            game.StartGame();
            yield return EndIntro();

            // Один толчок — подсказка снята.
            fake.Next = new BackendSnapshot { CrankDeltaDegrees = GameConfig.CrankDegreesPerPush * 2f };
            PinCalm();
            yield return null;
            fake.Next = new BackendSnapshot();
            Assert.IsFalse(game.AwaitingFirstPush, "предусловие: игрок уже толкал камень");

            // Поражение: силы кончились. Весь проигрыш игрок проводит ничего не делая.
            game.Stamina = 0f;
            float guard = 0f;
            while (game.State != GState.Over && guard < 30f)
            {
                guard += Mathf.Min(Time.deltaTime, 0.05f);
                fake.Next = new BackendSnapshot();
                yield return null;
            }
            Assert.AreEqual(GState.Over, game.State, "предусловие: доиграли сцену поражения до экрана");
            Assert.IsFalse(ui.CrankHintVisible,
                "на экране проигрыша крутилка не слушается — подсказка «крути» там была бы ложью");

            // Зелёная — заново; снова вступление, снова ничего делать не надо.
            game.StartGame();
            yield return EndIntro();

            Assert.IsTrue(ui.CrankHintVisible,
                "после поражения подсказка обязана вернуться — это и есть жалоба со стойки");
        }
    }
}
