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
            }
            EnsureAudioListener();

            var go = new GameObject("EndlessSisyphus");
            var game = go.AddComponent<SisyphusGame>();   // Awake создаёт WorldRenderer/AudioEngine и настраивает камеру
            var quotes = go.AddComponent<QuoteDirector>();
            quotes.game = game;
            var ui = go.AddComponent<GameUI>();
            ui.game = game;
            ui.quotes = quotes;
        }

        /// <summary>
        /// Гарантия слышимости: весь звук игры процедурный (AudioEngine синтезирует клипы и вешает
        /// AudioSource'ы на объект игры), а Unity воспроизводит AudioSource ТОЛЬКО при живом
        /// AudioListener в сцене. Раньше слушатель ставился внутри ветки «камеры нет» — то есть звук
        /// был у игры лишь пока она сама владеет камерой. В аркадном лаунчере сцену грузит хаб, и
        /// камера может прийти НЕ от нас (у хабовой камеры своего AudioListener нет — attract-видео
        /// звучит мимо звуковой системы Unity, через VideoAudioOutputMode.Direct): тогда в сцене нет
        /// ни одного слушателя и игра оказывается полностью немой при работающем звуке в standalone.
        /// Поэтому слушателя обеспечиваем независимо от того, чья камера, и снимаем глобальную паузу
        /// звука, которую могла оставить предыдущая игра в том же процессе лаунчера.
        /// </summary>
        public static void EnsureAudioListener()
        {
            AudioListener.pause = false;
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            for (int i = 0; i < listeners.Length; i++)
            {
                if (!listeners[i].gameObject.activeInHierarchy) continue;
                listeners[i].enabled = true;
                return;
            }
            var cam = Camera.main;
            var host = cam != null ? cam.gameObject : new GameObject("EndlessSisyphus_AudioListener");
            host.AddComponent<AudioListener>();
        }
    }
}
