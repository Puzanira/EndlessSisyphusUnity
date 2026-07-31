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
        // True only standalone, where nothing else set up ArcadeInput. In the arcade hub the
        // launcher owns the backend (keyboard + serial boards) and pumps it process-wide; replacing
        // it here would silently downgrade the game to keyboard-only and kill the physical controls.
        static bool ownsBackend;

        /// <summary>
        /// Инициализация бэкенда ТОЛЬКО если его ещё нет (standalone). В лаунчере бэкенд уже
        /// стоит (клавиатура + платы) — его не трогаем и не пампим (лаунчер пампит сам).
        /// Null-safe для headless: сам опрос устройств живёт в KeyboardBackend, который
        /// корректно переживает отсутствие Keyboard.current / Mouse.current.
        /// </summary>
        public static void Initialize()
        {
            ownsBackend = ArcadeInput.Backend == null;
            if (!ownsBackend) return;

            KeyboardMapping map = KeyboardMapping.LoadDefault();
            ArcadeInput.Initialize(new KeyboardBackend(map));
        }

        /// <summary>Опрос бэкенда раз в кадр (только когда бэкенд наш) — вызывать ДО чтения контролов.</summary>
        public static void Pump(float deltaTime)
        {
            if (ownsBackend) ArcadeInput.Update(deltaTime);
        }
    }
}
