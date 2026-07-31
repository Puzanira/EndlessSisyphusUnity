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

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Чистый лист: сцена теста без слушателя — как сцена игры, загруженная лаунчером.
            foreach (var l in Object.FindObjectsByType<AudioListener>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(l);
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (foreignCamera != null) Object.Destroy(foreignCamera);
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

            Assert.AreEqual(0, Object.FindObjectsByType<AudioListener>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                "предусловие: слушателя в сцене нет");

            Bootstrap.EnsureAudioListener();

            var listeners = Object.FindObjectsByType<AudioListener>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
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
            var host = new GameObject("SisyphusGame_Audio");
            ArcadeInput.Initialize(null);   // см. комментарий в SpeedCapTests: сброс бэкенда до Awake
            var game = host.AddComponent<SisyphusGame>();
            ArcadeInput.Initialize(new FakeBackend());
            yield return null;

            Assert.AreEqual(5, host.GetComponents<AudioSource>().Length,
                "AudioEngine поднимает свои AudioSource'ы на объекте игры (дрон/мелодия/sfx/ветер/кино)");

            game.Audio.ToggleMute();
            Assert.AreEqual(0f, AudioListener.volume, 1e-4f, "мьют глушит глобальную громкость");

            game.Audio.StopAll();   // контрактный выход по MenuButton
            Assert.AreEqual(1f, AudioListener.volume, 1e-4f,
                "выход из игры не должен уносить с собой звук лаунчера и следующей игры");

            Object.Destroy(host);
            var world = GameObject.Find("EndlessSisyphus_World");
            if (world != null) Object.Destroy(world);
            yield return null;
        }
    }
}
