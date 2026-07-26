using UnityEngine;
using UnityEngine.SceneManagement;

namespace EndlessSisyphus
{
    /// <summary>
    /// Автозапуск без настройки сцены: строит камеру (если нет), объект игры (SisyphusGame) и UI (GameUI).
    /// Импортировал .unitypackage → нажал Play. Если хочешь ручную настройку — удали этот файл и повесь
    /// SisyphusGame + GameUI на объект сам.
    ///
    /// Arcade-input: когда игру подключают ПАКЕТОМ к аркадному лаунчеру, её сцена — лишь одна из многих.
    /// <c>RuntimeInitializeOnLoadMethod</c> срабатывает ОДИН раз на старте плей-мода (в первой загруженной
    /// сцене — например, в меню лаунчера), поэтому «голый» автозапуск (а) построил бы Сизифа поверх чужой
    /// сцены и (б) не построил бы игру, когда её сцену грузят из лаунчера ПОЗЖЕ (одноразовый хук не
    /// повторяется). Поэтому здесь: подписываемся на <see cref="SceneManager.sceneLoaded"/> и строим игру
    /// ТОЛЬКО в собственной сцене (путь ассета содержит «EndlessSisyphus» — устойчиво к тому, что другая
    /// игра тоже зовёт свою сцену «Main»).
    /// </summary>
    public static class Bootstrap
    {
        // Устойчивый маркер собственной сцены (регистронезависимо): «sisyphus» есть в пути Main.unity и в
        // проекте (Assets/EndlessSisyphus/...), и как пакет (Packages/com.aigamestudio.game-endless-sisyphus/...).
        const string SceneMarker = "sisyphus";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryBuild(SceneManager.GetActiveScene());
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryBuild(scene);

        static void TryBuild(Scene scene)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path)
                || scene.path.ToLowerInvariant().IndexOf(SceneMarker, System.StringComparison.Ordinal) < 0)
                return;
            if (Object.FindAnyObjectByType<SisyphusGame>() != null) return;

            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            var go = new GameObject("EndlessSisyphus");
            var game = go.AddComponent<SisyphusGame>();   // Awake создаёт WorldRenderer/AudioEngine и настраивает камеру
            var quotes = go.AddComponent<QuoteDirector>();
            quotes.game = game;
            var ui = go.AddComponent<GameUI>();
            ui.game = game;
            ui.quotes = quotes;
        }
    }
}
