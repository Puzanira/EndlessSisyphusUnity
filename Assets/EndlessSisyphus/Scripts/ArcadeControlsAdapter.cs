using AiGameStudio.ArcadeControls;

namespace EndlessSisyphus
{
    /// <summary>
    /// Тонкий адаптер жизненного цикла arcade-controls для игры. Игровой код читает ввод
    /// ТОЛЬКО через <see cref="ArcadeInput"/>.* (контракт автомата) — прямых обращений к
    /// UnityEngine.Input / InputSystem в игре нет. Без железа работает клавиатурная симуляция
    /// пакета (маппинг в его конфиге: крутилка = колесо мыши).
    /// </summary>
    public static class ArcadeControlsAdapter
    {
        /// <summary>
        /// Свежая инициализация бэкенда + сброс всех логических контролов. Зовётся из Awake
        /// игры, поэтому каждый (повторный) Bootstrap стартует с чистым состоянием ввода.
        /// Null-safe для headless: сам опрос устройств живёт в KeyboardBackend, который
        /// корректно переживает отсутствие Keyboard.current / Mouse.current.
        /// </summary>
        public static void Initialize()
        {
            KeyboardMapping map = KeyboardMapping.LoadDefault();
            ArcadeInput.Initialize(new KeyboardBackend(map));
        }

        /// <summary>Опрос бэкенда раз в кадр — вызывать ДО чтения контролов игрой.</summary>
        public static void Pump(float deltaTime) => ArcadeInput.Update(deltaTime);
    }
}
