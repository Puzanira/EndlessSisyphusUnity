using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AiGameStudio.ArcadeControls;

namespace EndlessSisyphus.Tests
{
    /// <summary>
    /// Гарды слышимости игры в аркадном лаунчере (жалоба основательницы «в игре есть звук, а в
    /// нашей сборке его нет»). Весь звук процедурный: AudioEngine синтезирует клипы и вешает
    /// AudioSource'ы на объект игры — внешних аудио-ассетов нет. Значит слышимость держится ровно
    /// на двух глобальных вещах, которыми игра в лаунчере НЕ владеет:
    ///  - AudioListener в сцене (сцену грузит хаб, камера может быть не наша);
    ///  - глобальные AudioListener.volume / AudioListener.pause (общие на весь процесс лаунчера).
    /// </summary>
    public class ArcadeAudioPathTests
    {
        GameObject foreignCamera;
        GameObject gameHost;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Чистый лист: сцена теста без слушателя — как сцена игры, загруженная лаунчером.
            foreach (var l in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include))
                Object.DestroyImmediate(l);
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            yield return null;
        }

        // Уборка ТОЛЬКО здесь: упавший Assert обрывает тело теста, и живой SisyphusGame,
        // снесённый бы в конце теста, утёк бы во все следующие фикстуры (его Update
        // продолжает рендерить мир и валит их чужими исключениями).
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (foreignCamera != null) Object.Destroy(foreignCamera);
            if (gameHost != null) Object.Destroy(gameHost);
            var world = GameObject.Find("EndlessSisyphus_World");
            if (world != null) Object.Destroy(world);
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator ForeignMainCamera_StillLeavesGameWithAudioListener()
        {
            // Камера уже есть и она НЕ наша (лаунчер/предыдущая сцена), слушателя на ней нет —
            // ровно та ситуация, в которой игра оказывалась немой при живом звуке в standalone.
            foreignCamera = new GameObject("LauncherCamera", typeof(Camera));
            foreignCamera.tag = "MainCamera";
            yield return null;

            Assert.AreEqual(0, Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include).Length,
                "предусловие: слушателя в сцене нет");

            Bootstrap.EnsureAudioListener();

            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            Assert.AreEqual(1, listeners.Length,
                "игра обязана обеспечить ровно один AudioListener, даже когда камерой владеет лаунчер");
            Assert.IsTrue(listeners[0].enabled && listeners[0].gameObject.activeInHierarchy,
                "слушатель должен быть живым, иначе звука не будет");
            Assert.IsFalse(AudioListener.pause,
                "глобальная пауза звука не должна доставаться игре от предыдущей игры лаунчера");
        }

        [UnityTest]
        public IEnumerator Exit_RestoresGlobalVolume_ForNextGame()
        {
            gameHost = new GameObject("SisyphusGame_Audio");
            ArcadeInput.Initialize(null);   // см. комментарий в SpeedCapTests: сброс бэкенда до Awake
            var game = gameHost.AddComponent<SisyphusGame>();
            ArcadeInput.Initialize(new FakeBackend());
            yield return null;

            // Ровное число источников — деталь автора (он добавляет их по мере роста звука,
            // сейчас дрон/мелодия/sfx/ветер/дождь/кино), поэтому проверяем инвариант:
            // движок поднимает свои источники НА ОБЪЕКТЕ ИГРЫ, а не где-то в сцене.
            Assert.GreaterOrEqual(gameHost.GetComponents<AudioSource>().Length, 5,
                "AudioEngine поднимает свои AudioSource'ы на объекте игры");

            game.Audio.ToggleMute();
            Assert.AreEqual(0f, AudioListener.volume, 1e-4f, "мьют глушит глобальную громкость");

            game.Audio.StopAll();   // контрактный выход по MenuButton
            Assert.AreEqual(1f, AudioListener.volume, 1e-4f,
                "выход из игры не должен уносить с собой звук лаунчера и следующей игры");
            yield return null;
        }
    }
}
