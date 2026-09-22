using UnityEngine;

namespace EndlessSisyphus
{
    /// <summary>
    /// Экраны и HUD на IMGUI (OnGUI) — без зависимости от uGUI/TMP и без настройки сцены.
    /// Порт HTML-оверлеев: старт «Как преодолевать препятствия», HUD, настройки, Game Over.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public SisyphusGame game;
        public QuoteDirector quotes;

        const string DefeatQuote = "«Сизиф, бессильный и бунтующий, знает о бесконечности своей печальной участи»";
        /// <summary>Подсказка первого действия. «Крутилка» — каноничное имя органа стойки;
        /// формулировка основательницы связывает орган с последствием («чтобы толкать
        /// камень»), а не просто командует. Направление вращения в ней не названо —
        /// код его и не проверяет (крутить можно в любую сторону).</summary>
        public const string CrankStartHint = "Крути крутилку, чтобы толкать камень.";

        /// <summary>Подписи кнопок экранов. Вынесены в константы не ради переиспользования —
        /// каждая используется ровно один раз, — а чтобы тест подсветки имён органов
        /// (<c>HintHighlightTests</c>) проверял ту же строку, которую игрок читает на экране,
        /// а не свою копию. Слова не менялись.</summary>
        public const string StartButtonLabel = "ЗЕЛЁНАЯ КНОПКА — НАЧАТЬ";
        /// <inheritdoc cref="StartButtonLabel"/>
        public const string RestartButtonLabel = "ЗЕЛЁНАЯ КНОПКА — ЗАНОВО";

        /// <summary>
        /// Эпиграф. Со стартового экрана снят (f7479a6) и по решению основательницы
        /// переехал в начало забега: он идёт поверх вступительной анимации, пока Сизиф
        /// подходит к камню, и гаснет ровно к её концу — там, где оживает крутилка и
        /// загорается <see cref="CrankStartHint"/>. Подача — тот же каменный слой, что у
        /// цитат Камю по ходу подъёма (DrawStoneQuote), чтобы он читался как часть
        /// литературного слоя игры, а не как вставка чужого экрана.
        /// </summary>
        public const string Epigraph =
            "«Боги приговорили Сизифа вечно вкатывать на вершину горы камень, который, едва достигнув цели, скатывался вниз»";

        Texture2D white, marble, instructionTablet, quoteTablet, authorLogo, buttonNormal, buttonHover, buttonActive;
        Texture2D secondaryButtonNormal, secondaryButtonHover, secondaryButtonActive;
        Font displayFont, uiFont, uiStrongFont, uiBoldFont;
        GUIStyle title, h2, body, hint, btn, banner, quote;
        float UiScale => Mathf.Clamp(
            Mathf.Min(Screen.width / 1280f, Screen.height / 720f),
            0.78f, 1.5f);

        void Awake()
        {
            white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply();
            authorLogo = CreateAuthorLogoTexture();
            marble = CreateMarbleTexture();
            instructionTablet = CreateInstructionTabletTexture();
            quoteTablet = CreateQuoteTabletTexture(instructionTablet);
            buttonNormal = CreateButtonTexture(new Color32(46, 42, 58, 244), new Color32(115, 91, 55, 255), new Color32(85, 73, 66, 255));
            buttonHover = CreateButtonTexture(new Color32(57, 49, 67, 250), new Color32(202, 158, 72, 255), new Color32(121, 96, 68, 255));
            buttonActive = CreateButtonTexture(new Color32(35, 31, 48, 250), new Color32(229, 181, 79, 255), new Color32(76, 62, 57, 255));
            secondaryButtonNormal = CreateButtonTexture(new Color32(35, 32, 46, 218), new Color32(76, 70, 83, 210), new Color32(55, 51, 64, 190), 1);
            secondaryButtonHover = CreateButtonTexture(new Color32(43, 39, 53, 230), new Color32(137, 112, 69, 225), new Color32(80, 68, 60, 205), 1);
            secondaryButtonActive = CreateButtonTexture(new Color32(29, 27, 39, 230), new Color32(161, 127, 67, 230), new Color32(61, 53, 52, 205), 1);
            if (quotes == null) quotes = GetComponent<QuoteDirector>();
        }

        void EnsureStyles()
        {
            if (title != null) return;

            displayFont = Resources.Load<Font>("Fonts/PressStart2P-Regular");
            uiFont = Resources.Load<Font>("Fonts/Jura-Medium");
            uiStrongFont = Resources.Load<Font>("Fonts/Jura-SemiBold");
            uiBoldFont = Resources.Load<Font>("Fonts/Jura-Bold");

            if (displayFont == null || uiFont == null || uiStrongFont == null || uiBoldFont == null)
            {
                Debug.LogError("Не удалось загрузить один или несколько встроенных шрифтов Endless Sisyphus.");
            }
            if (uiFont == null) uiFont = GUI.skin.font;
            if (uiStrongFont == null) uiStrongFont = uiFont;
            if (uiBoldFont == null) uiBoldFont = uiStrongFont;
            if (displayFont == null) displayFont = uiStrongFont;

            title = new GUIStyle(GUI.skin.label) { font = displayFont, fontSize = 34, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.8f, 0.2f) } };
            h2 = new GUIStyle(GUI.skin.label) { font = uiStrongFont, fontSize = 22, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.84f, 0.29f, 0.23f) } };
            body = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 14, fontStyle = FontStyle.Normal, wordWrap = true, normal = { textColor = new Color(0.96f, 0.91f, 0.82f) } };
            hint = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 12, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.6f, 0.55f, 0.42f) } };
            btn = new GUIStyle(GUI.skin.button)
            {
                font = uiStrongFont,
                fontSize = 16,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(3, 3, 3, 3),
                padding = new RectOffset(12, 12, 6, 6),
                normal = { background = buttonNormal, textColor = new Color(0.82f, 0.76f, 0.63f) },
                hover = { background = buttonHover, textColor = new Color(1f, 0.83f, 0.34f) },
                active = { background = buttonActive, textColor = new Color(1f, 0.76f, 0.24f) },
                focused = { background = buttonHover, textColor = new Color(1f, 0.83f, 0.34f) }
            };
            banner = new GUIStyle(GUI.skin.box) { font = uiFont, fontSize = 14, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };

            quote = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                fontSize = 22,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(0.19f, 0.20f, 0.20f) }
            };

            LockPassiveStates(title);
            LockPassiveStates(h2);
            LockPassiveStates(body);
            LockPassiveStates(hint);
            LockPassiveStates(banner);
            LockPassiveStates(quote);
        }

        GUIStyle SecondaryButtonStyle(int fontSize)
        {
            var style = new GUIStyle(btn)
            {
                fontSize = fontSize,
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(8, 8, 3, 3)
            };
            style.normal.background = secondaryButtonNormal;
            style.normal.textColor = new Color(0.61f, 0.59f, 0.57f);
            style.hover.background = secondaryButtonHover;
            style.hover.textColor = new Color(0.86f, 0.76f, 0.52f);
            style.active.background = secondaryButtonActive;
            style.active.textColor = new Color(0.94f, 0.78f, 0.40f);
            style.focused.background = secondaryButtonHover;
            style.focused.textColor = style.hover.textColor;
            return style;
        }

        static void LockPassiveStates(GUIStyle style)
        {
            Color textColor = style.normal.textColor;
            Texture2D background = style.normal.background;
            style.hover.textColor = textColor;
            style.hover.background = background;
            style.active.textColor = textColor;
            style.active.background = background;
            style.focused.textColor = textColor;
            style.focused.background = background;
            style.onNormal.textColor = textColor;
            style.onNormal.background = background;
            style.onHover.textColor = textColor;
            style.onHover.background = background;
            style.onActive.textColor = textColor;
            style.onActive.background = background;
            style.onFocused.textColor = textColor;
            style.onFocused.background = background;
        }

        /// <summary>
        /// Кнопка экрана, в подписи которой имя цветной кнопки стойки покрашено в её цвет
        /// («ЗЕЛЁНАЯ КНОПКА — НАЧАТЬ»).
        ///
        /// Подпись рисуется отдельно от кнопки: GUI.Button красит свой текст целиком одним
        /// цветом состояния, а покрасить надо кусок. Поэтому кнопка рисуется пустой (фон,
        /// рамка и клик — её), а текст кладётся поверх посимвольно тем же способом, что и
        /// на плашках подсказок. Указателя у стойки нет, кнопка всегда в состоянии normal,
        /// так что цвет подписи берём из него.
        /// </summary>
        bool OrganButton(Rect rect, string text, GUIStyle style)
        {
            bool pressed = GUI.Button(rect, GUIContent.none, style);
            Color baseColor = style.normal.textColor;
            var labelStyle = PassiveText(new GUIStyle(GUI.skin.label)
            {
                font = style.font,
                fontSize = style.fontSize,
                fontStyle = style.fontStyle,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
                normal = { textColor = baseColor }
            });
            TrackedLabel(rect, text, labelStyle, 0f, HintTextColors(text, baseColor));
            return pressed;
        }

        static GUIStyle PassiveText(GUIStyle source)
        {
            var style = new GUIStyle(source);
            LockPassiveStates(style);
            return style;
        }

        static void PassiveLabel(Rect rect, string text, GUIStyle style) =>
            GUI.Label(rect, text, PassiveText(style));

        void Rect2(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = o; }

        static bool IsRightAligned(TextAnchor alignment) =>
            alignment == TextAnchor.UpperRight ||
            alignment == TextAnchor.MiddleRight ||
            alignment == TextAnchor.LowerRight;

        static bool IsCentered(TextAnchor alignment) =>
            alignment == TextAnchor.UpperCenter ||
            alignment == TextAnchor.MiddleCenter ||
            alignment == TextAnchor.LowerCenter;

        static TextAnchor LeftAligned(TextAnchor alignment)
        {
            if (alignment == TextAnchor.MiddleLeft ||
                alignment == TextAnchor.MiddleCenter ||
                alignment == TextAnchor.MiddleRight) return TextAnchor.MiddleLeft;
            if (alignment == TextAnchor.LowerLeft ||
                alignment == TextAnchor.LowerCenter ||
                alignment == TextAnchor.LowerRight) return TextAnchor.LowerLeft;
            return TextAnchor.UpperLeft;
        }

        void TrackedLabel(Rect rect, string text, GUIStyle style, float tracking) =>
            TrackedLabel(rect, text, style, tracking, null);

        /// <summary>
        /// Строка посимвольно, с трекингом и — необязательно — со своим цветом у каждого
        /// символа (<paramref name="colors"/>; null = вся строка цветом стиля).
        ///
        /// Посимвольный цвет здесь не роскошь, а единственный доступный способ: имена
        /// цветных кнопок стойки красятся НА ОТРИСОВКЕ, а не разметкой в тексте (почему —
        /// см. <see cref="HintTextColors"/>), и красить надо кусок строки, а не строку
        /// целиком. Метрика от цвета не зависит, поэтому раскладка плашек не едет.
        /// </summary>
        void TrackedLabel(Rect rect, string text, GUIStyle style, float tracking, Color[] colors)
        {
            var glyphStyle = new GUIStyle(style)
            {
                alignment = LeftAligned(style.alignment),
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            LockPassiveStates(glyphStyle);

            float totalWidth = 0f;
            for (int i = 0; i < text.Length; i++)
                totalWidth += glyphStyle.CalcSize(new GUIContent(text[i].ToString())).x;
            totalWidth += Mathf.Max(0, text.Length - 1) * tracking;

            float x = rect.x;
            if (IsCentered(style.alignment)) x += (rect.width - totalWidth) * 0.5f;
            else if (IsRightAligned(style.alignment)) x += rect.width - totalWidth;

            for (int i = 0; i < text.Length; i++)
            {
                if (colors != null && i < colors.Length && glyphStyle.normal.textColor != colors[i])
                {
                    glyphStyle.normal.textColor = colors[i];
                    LockPassiveStates(glyphStyle);
                }
                string character = text[i].ToString();
                float glyphWidth = glyphStyle.CalcSize(new GUIContent(character)).x;
                GUI.Label(new Rect(x, rect.y, glyphWidth + tracking + 3f, rect.height), character, glyphStyle);
                x += glyphWidth + tracking;
            }
        }

        static Rect PixelRect(Rect rect, float pixelScale) => new Rect(
            Mathf.Round(rect.x / pixelScale),
            Mathf.Round(rect.y / pixelScale),
            Mathf.Round(rect.width / pixelScale),
            Mathf.Round(rect.height / pixelScale));

        void PreparePixelFont(Font font)
        {
            if (font != null && font.material != null && font.material.mainTexture != null)
                font.material.mainTexture.filterMode = FilterMode.Point;
        }

        void PixelTrackedLabel(Rect rect, string text, GUIStyle style, float tracking, float pixelScale)
        {
            PreparePixelFont(style.font);
            var pixelStyle = new GUIStyle(style)
            {
                fontSize = Mathf.Max(1, Mathf.RoundToInt(style.fontSize / pixelScale))
            };
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(pixelScale, pixelScale, 1f)) * oldMatrix;
            TrackedLabel(PixelRect(rect, pixelScale), text, pixelStyle,
                Mathf.Round(tracking / pixelScale));
            GUI.matrix = oldMatrix;
        }

        void PixelPassiveLabel(Rect rect, string text, GUIStyle style, float pixelScale)
        {
            PreparePixelFont(style.font);
            var pixelStyle = PassiveText(new GUIStyle(style)
            {
                fontSize = Mathf.Max(1, Mathf.RoundToInt(style.fontSize / pixelScale))
            });
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(pixelScale, pixelScale, 1f)) * oldMatrix;
            GUI.Label(PixelRect(rect, pixelScale), text, pixelStyle);
            GUI.matrix = oldMatrix;
        }

        /// <summary>
        /// Строка с обводкой. Обводка всегда одноцветная (<paramref name="strokeColor"/>) —
        /// она отрывает глиф от фона; цвет несёт только сама строка, и именно поэтому
        /// <paramref name="colors"/> применяется лишь к последнему, «лицевому» проходу.
        ///
        /// Параметр обязательный, без перегрузки без него: подсветка имён кнопок обязана
        /// доезжать до отрисовки, а не теряться молча на полпути.
        /// </summary>
        void TrackedOutlinedLabel(Rect rect, string text, GUIStyle style, float tracking, float stroke,
            Color strokeColor, Color[] colors)
        {
            var outlineStyle = PassiveText(new GUIStyle(style));
            outlineStyle.normal.textColor = strokeColor;
            TrackedLabel(new Rect(rect.x - stroke, rect.y, rect.width, rect.height), text, outlineStyle, tracking);
            TrackedLabel(new Rect(rect.x + stroke, rect.y, rect.width, rect.height), text, outlineStyle, tracking);
            TrackedLabel(new Rect(rect.x, rect.y - stroke, rect.width, rect.height), text, outlineStyle, tracking);
            TrackedLabel(new Rect(rect.x, rect.y + stroke, rect.width, rect.height), text, outlineStyle, tracking);
            TrackedLabel(rect, text, style, tracking, colors);
        }

        float TrackedTextWidth(string text, GUIStyle style, float tracking)
        {
            float width = 0f;
            for (int i = 0; i < text.Length; i++)
                width += style.CalcSize(new GUIContent(text[i].ToString())).x;
            return width + Mathf.Max(0, text.Length - 1) * tracking;
        }

        void DrawMetric(Rect rect, string label, string value, GUIStyle labelStyle, GUIStyle valueStyle,
            float labelTracking, float valueTracking, float gap, bool alignRight)
        {
            var left = PassiveText(new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleLeft });
            var right = PassiveText(new GUIStyle(valueStyle) { alignment = TextAnchor.MiddleLeft });
            float labelWidth = TrackedTextWidth(label, left, labelTracking);
            float valueWidth = TrackedTextWidth(value, right, valueTracking);
            float total = labelWidth + gap + valueWidth;
            // Старт метрики кладём на целую координату: на пиксельном пути (см. PixelDrawMetric)
            // половинка единицы — это половина «крупного» пикселя, и весь блок расплывается.
            float x = Mathf.Round(alignRight ? rect.xMax - total : rect.x + (rect.width - total) * 0.5f);
            TrackedLabel(new Rect(x, rect.y, labelWidth, rect.height), label, left, labelTracking);
            TrackedLabel(new Rect(x + labelWidth + gap, rect.y, valueWidth, rect.height), value, right, valueTracking);
        }

        /// <summary>
        /// Пиксельный путь для метрики «подпись + значение» — то же, что <see cref="PixelTrackedLabel"/>
        /// делает для одной строки. Пиксельный шрифт (PressStart2P) чёткий только тогда, когда его
        /// растеризуют в мелком «логическом» кегле, а на экран увеличивают целой матрицей GUI: при
        /// прямой отрисовке крупным кеглем Unity растягивает сглаженную битмапу атласа, и строка
        /// выглядит мыльной рядом с заголовком, который через пиксельный путь уже идёт.
        /// </summary>
        void PixelDrawMetric(Rect rect, string label, string value, GUIStyle labelStyle, GUIStyle valueStyle,
            float labelTracking, float valueTracking, float gap, bool alignRight, float pixelScale)
        {
            PreparePixelFont(labelStyle.font);
            PreparePixelFont(valueStyle.font);
            var pixelLabel = new GUIStyle(labelStyle)
            {
                fontSize = Mathf.Max(1, Mathf.RoundToInt(labelStyle.fontSize / pixelScale))
            };
            var pixelValue = new GUIStyle(valueStyle)
            {
                fontSize = Mathf.Max(1, Mathf.RoundToInt(valueStyle.fontSize / pixelScale))
            };
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(pixelScale, pixelScale, 1f)) * oldMatrix;
            DrawMetric(PixelRect(rect, pixelScale), label, value, pixelLabel, pixelValue,
                Mathf.Round(labelTracking / pixelScale), Mathf.Round(valueTracking / pixelScale),
                Mathf.Round(gap / pixelScale), alignRight);
            GUI.matrix = oldMatrix;
        }

        void DrawTitleDivider(float centerX, float y, float width, float scale)
        {
            Color gold = new Color(0.91f, 0.81f, 0.48f, 0.82f);
            float gap = 8f * scale;
            float half = width * 0.5f;
            Rect2(new Rect(centerX - half, y, half - gap, 1f), gold);
            Rect2(new Rect(centerX + gap, y, half - gap, 1f), gold);

            var oldMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, new Vector2(centerX, y + 0.5f));
            Rect2(new Rect(centerX - 2.5f * scale, y - 2f * scale, 5f * scale, 5f * scale), gold);
            GUI.matrix = oldMatrix;
        }

        Texture2D CreateButtonTexture(Color32 fill, Color32 border, Color32 highlight, int edgeSize = 2)
        {
            const int size = 12;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < edgeSize || x >= size - edgeSize || y < edgeSize || y >= size - edgeSize;
                    bool topEdge = y >= size - edgeSize - 1 && x >= edgeSize && x < size - edgeSize;
                    pixels[y * size + x] = edge ? border : topEdge ? highlight : fill;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            return texture;
        }

        Texture2D CreateAuthorLogoTexture()
        {
            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Color32 stone = new Color32(194, 179, 135, 255);
            Color32 stoneShadow = new Color32(135, 123, 105, 255);
            Color32 curls = new Color32(74, 66, 70, 255);
            Color32 curlLight = new Color32(112, 98, 91, 255);

            void Plot(int x, int y, Color32 color)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                    pixels[y * size + x] = color;
            }

            void Block(int x, int y, int width, int height, Color32 color)
            {
                for (int yy = y; yy < y + height; yy++)
                    for (int xx = x; xx < x + width; xx++)
                        Plot(xx, yy, color);
            }

            void Disc(int cx, int cy, int radius, Color32 color)
            {
                int rr = radius * radius;
                for (int y = cy - radius; y <= cy + radius; y++)
                    for (int x = cx - radius; x <= cx + radius; x++)
                    {
                        int dx = x - cx;
                        int dy = y - cy;
                        if (dx * dx + dy * dy <= rr) Plot(x, y, color);
                    }
            }

            // Симметричная каменная голова анфас.
            Block(13, 2, 25, 5, stoneShadow);
            Block(20, 6, 12, 10, stone);
            for (int y = 12; y <= 38; y++)
            {
                float dy = (y - 25f) / 14f;
                for (int x = 12; x <= 36; x++)
                {
                    float dx = (x - 24f) / 11f;
                    if (dx * dx + dy * dy <= 1f) Plot(x, y, stone);
                }
            }
            Disc(12, 25, 3, stoneShadow);
            Disc(36, 25, 3, stoneShadow);
            Plot(19, 28, new Color32(45, 42, 48, 255));
            Plot(29, 28, new Color32(45, 42, 48, 255));
            Block(23, 21, 3, 7, stoneShadow); // прямой нос
            Block(21, 18, 7, 1, stoneShadow); // спокойная линия губ
            Block(19, 13, 10, 2, stone);      // чистый подбородок без бороды

            // Кудри симметрично обрамляют верх головы, не заходя на подбородок.
            int[,] curlCenters =
            {
                {13, 31}, {15, 37}, {20, 41}, {24, 42},
                {28, 41}, {33, 37}, {35, 31}, {18, 35}, {30, 35}
            };
            for (int i = 0; i < curlCenters.GetLength(0); i++)
            {
                int cx = curlCenters[i, 0];
                int cy = curlCenters[i, 1];
                Disc(cx, cy, 4, curls);
                Disc(cx - 1, cy + 1, 1, curlLight);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            return texture;
        }

        Texture2D CreateMarbleTexture()
        {
            const int w = 128, h = 64;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            var random = new System.Random(1975);
            float ox = (float)random.NextDouble() * 20f;
            float oy = (float)random.NextDouble() * 20f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float broad = Mathf.PerlinNoise(ox + x * 0.035f, oy + y * 0.08f);
                    float grain = Mathf.PerlinNoise(ox + x * 0.21f, oy + y * 0.25f);
                    float veinWave = Mathf.Sin(x * 0.055f + y * 0.22f + broad * 5f);
                    float vein = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(veinWave)), 9f);
                    float value = 0.27f + (broad - 0.5f) * 0.065f + (grain - 0.5f) * 0.020f - vein * 0.026f;
                    pixels[y * w + x] = new Color(value * 1.02f, value * 0.96f, value, 1f);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Point;
            return texture;
        }

        Texture2D CreateInstructionTabletTexture()
        {
            const int width = 512;
            const int height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            var random = new System.Random(2027);
            float ox = (float)random.NextDouble() * 18f;
            float oy = (float)random.NextDouble() * 18f;
            var outline = new[]
            {
                new Vector2(20f, 8f), new Vector2(488f, 8f),
                new Vector2(488f, 12f), new Vector2(496f, 12f),
                new Vector2(496f, 20f), new Vector2(504f, 20f),
                new Vector2(504f, 232f), new Vector2(500f, 232f),
                new Vector2(500f, 240f), new Vector2(492f, 240f),
                new Vector2(492f, 248f), new Vector2(20f, 248f),
                new Vector2(20f, 244f), new Vector2(12f, 244f),
                new Vector2(12f, 236f), new Vector2(8f, 236f),
                new Vector2(8f, 20f), new Vector2(12f, 20f),
                new Vector2(12f, 12f), new Vector2(20f, 12f)
            };

            Color marbleTop = new Color32(146, 150, 166, 255);
            Color marbleBottom = new Color32(109, 115, 133, 255);
            Color nightOverlay = new Color32(44, 42, 61, 255);
            Color veinColor = new Color32(86, 91, 110, 255);
            Color crackColor = new Color32(74, 77, 92, 255);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    if (!PointInPolygon(point, outline))
                    {
                        pixels[y * width + x] = Color.clear;
                        continue;
                    }
                    float silhouetteDistance = PolygonEdgeDistance(point, outline);

                    float vertical = y / (height - 1f);
                    Color stone = Color.Lerp(marbleTop, marbleBottom, vertical);
                    float broad = Mathf.PerlinNoise(ox + x * 0.021f, oy + y * 0.034f) - 0.5f;
                    float grain = Mathf.PerlinNoise(ox + x * 0.15f, oy + y * 0.17f) - 0.5f;
                    float naturalVariation = broad * 0.035f + grain * 0.012f;
                    stone.r = Mathf.Clamp01(stone.r + naturalVariation);
                    stone.g = Mathf.Clamp01(stone.g + naturalVariation);
                    stone.b = Mathf.Clamp01(stone.b + naturalVariation * 1.1f);
                    float nightAmount = Mathf.Lerp(0.12f, 0.28f,
                        Mathf.Clamp01(vertical * 0.65f + Mathf.PerlinNoise(ox + x * 0.009f, oy + y * 0.012f) * 0.35f));
                    Color nightMultiplied = new Color(
                        stone.r * nightOverlay.r,
                        stone.g * nightOverlay.g,
                        stone.b * nightOverlay.b,
                        stone.a);
                    stone = Color.Lerp(stone, nightMultiplied, nightAmount);

                    float veinDistance = TabletVeinDistance(x, y);
                    if (veinDistance < 1.65f)
                        stone = Color.Lerp(stone, veinColor, (1f - veinDistance / 1.65f) * 0.25f);

                    float crackDistance = TabletCrackDistance(x, y);
                    if (crackDistance < 1.25f)
                        stone = Color.Lerp(stone, crackColor, (1f - crackDistance / 1.25f) * 0.68f);

                    stone.a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(silhouetteDistance / 1.35f));
                    pixels[y * width + x] = stone;
                }
            }

            pixels = PixelateTabletPixels(pixels, width, height, 4);
            texture.SetPixels(pixels);
            texture.Apply(false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            return texture;
        }

        static Color[] PixelateTabletPixels(Color[] source, int width, int height, int blockSize)
        {
            var result = new Color[source.Length];
            for (int blockY = 0; blockY < height; blockY += blockSize)
            {
                for (int blockX = 0; blockX < width; blockX += blockSize)
                {
                    Color average = Color.clear;
                    float alphaSum = 0f;
                    int samples = 0;
                    int maxY = Mathf.Min(blockY + blockSize, height);
                    int maxX = Mathf.Min(blockX + blockSize, width);

                    for (int y = blockY; y < maxY; y++)
                    {
                        for (int x = blockX; x < maxX; x++)
                        {
                            Color sample = source[y * width + x];
                            alphaSum += sample.a;
                            samples++;
                            if (sample.a <= 0.01f) continue;
                            average.r += sample.r * sample.a;
                            average.g += sample.g * sample.a;
                            average.b += sample.b * sample.a;
                            average.a += sample.a;
                        }
                    }

                    float coverage = samples > 0 ? alphaSum / samples : 0f;
                    Color blockColor = Color.clear;
                    if (coverage >= 0.46f && average.a > 0.001f)
                    {
                        blockColor = new Color(
                            average.r / average.a,
                            average.g / average.a,
                            average.b / average.a,
                            1f);
                    }

                    for (int y = blockY; y < maxY; y++)
                        for (int x = blockX; x < maxX; x++)
                            result[y * width + x] = blockColor;
                }
            }
            return result;
        }

        Texture2D CreateQuoteTabletTexture(Texture2D source)
        {
            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            Color[] pixels = source.GetPixels();
            Color lift = new Color(0.78f, 0.80f, 0.87f, 1f);
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a <= 0.001f) continue;
                float alpha = pixels[i].a;
                pixels[i] = Color.Lerp(pixels[i], lift, 0.22f);
                pixels[i].a = alpha;
            }
            texture.SetPixels(pixels);
            texture.Apply(false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            return texture;
        }

        static bool PointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        static float PolygonEdgeDistance(Vector2 point, Vector2[] polygon)
        {
            float distance = float.MaxValue;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Length];
                distance = Mathf.Min(distance, SegmentDistance(point.x, point.y, a.x, a.y, b.x, b.y));
            }
            return distance;
        }

        static float TabletVeinDistance(float x, float y)
        {
            float d = float.MaxValue;
            d = Mathf.Min(d, SegmentDistance(x, y, 91f, 12f, 112f, 34f));
            d = Mathf.Min(d, SegmentDistance(x, y, 112f, 34f, 148f, 48f));
            d = Mathf.Min(d, SegmentDistance(x, y, 112f, 34f, 129f, 63f));
            d = Mathf.Min(d, SegmentDistance(x, y, 322f, 246f, 317f, 222f));
            d = Mathf.Min(d, SegmentDistance(x, y, 317f, 222f, 346f, 201f));
            d = Mathf.Min(d, SegmentDistance(x, y, 317f, 222f, 290f, 207f));
            d = Mathf.Min(d, SegmentDistance(x, y, 497f, 86f, 473f, 93f));
            d = Mathf.Min(d, SegmentDistance(x, y, 473f, 93f, 450f, 113f));
            return d;
        }

        static float TabletCrackDistance(float x, float y)
        {
            float d = float.MaxValue;
            d = Mathf.Min(d, SegmentDistance(x, y, 482f, 26f, 458f, 49f));
            d = Mathf.Min(d, SegmentDistance(x, y, 458f, 49f, 430f, 61f));
            d = Mathf.Min(d, SegmentDistance(x, y, 458f, 49f, 468f, 77f));
            d = Mathf.Min(d, SegmentDistance(x, y, 29f, 220f, 55f, 202f));
            d = Mathf.Min(d, SegmentDistance(x, y, 55f, 202f, 83f, 190f));
            d = Mathf.Min(d, SegmentDistance(x, y, 55f, 202f, 65f, 226f));
            d = Mathf.Min(d, SegmentDistance(x, y, 490f, 151f, 463f, 149f));
            d = Mathf.Min(d, SegmentDistance(x, y, 463f, 149f, 439f, 137f));
            d = Mathf.Min(d, SegmentDistance(x, y, 463f, 149f, 447f, 169f));
            return d;
        }

        static float SegmentDistance(float px, float py, float ax, float ay, float bx, float by)
        {
            float abx = bx - ax;
            float aby = by - ay;
            float lengthSquared = abx * abx + aby * aby;
            float t = lengthSquared > 0f ? Mathf.Clamp01(((px - ax) * abx + (py - ay) * aby) / lengthSquared) : 0f;
            float dx = px - (ax + abx * t);
            float dy = py - (ay + aby * t);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // Помощника Frame(...) здесь больше нет: рамку рисовал только он и только у плашек
        // подсказок, а плашки по решению основательницы стали безрамочными (см. PaintHintPlate).

        void DrawBevel(Rect r, float thickness, Color light, Color dark)
        {
            Rect2(new Rect(r.x, r.y, r.width, thickness), light);
            Rect2(new Rect(r.x, r.y, thickness, r.height), light);
            Rect2(new Rect(r.x, r.yMax - thickness, r.width, thickness), dark);
            Rect2(new Rect(r.xMax - thickness, r.y, thickness, r.height), dark);
        }

        void DrawStonePanel(Rect r, float alpha)
        {
            float scale = UiScale;
            float panelAlpha = alpha * 0.98f;
            var old = GUI.color;
            GUI.color = new Color(0.08f, 0.07f, 0.12f, 0.42f * alpha);
            GUI.DrawTexture(new Rect(r.x + 5f * scale, r.y + 7f * scale, r.width, r.height),
                quoteTablet, ScaleMode.StretchToFill, true);
            GUI.color = new Color(1f, 1f, 1f, panelAlpha);
            GUI.DrawTexture(r, quoteTablet, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }

        Rect QuoteRect(string text, float alpha)
        {
            float scale = UiScale;
            float width = Mathf.Min(Screen.width * 0.74f, 900f * scale);
            int baseSize = text.Length > 170 ? 20 : text.Length > 105 ? 22 : 24;
            quote.fontSize = Mathf.RoundToInt(baseSize * scale);
            float textHeight = quote.CalcHeight(new GUIContent(text), width - 52f * scale);
            float height = Mathf.Clamp(textHeight + 42f * scale, 84f * scale, Mathf.Min(172f * scale, Screen.height * 0.23f));
            float slide = (1f - alpha) * 18f * scale;
            return new Rect((Screen.width - width) * 0.5f, Screen.height - height - 22f * scale + slide, width, height);
        }

        Rect DrawStoneQuote(string text, float alpha)
        {
            Rect r = QuoteRect(text, alpha);
            return DrawStoneQuoteAt(r, text, alpha);
        }

        Rect DrawStoneQuoteAt(Rect r, string text, float alpha)
        {
            DrawStonePanel(r, alpha);

            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            float scale = UiScale;
            Rect textRect = new Rect(r.x + 26f * scale, r.y + 16f * scale, r.width - 52f * scale, r.height - 32f * scale);
            quote.normal.textColor = new Color(0.012f, 0.010f, 0.015f);
            PassiveLabel(textRect, text, quote);
            GUI.color = old;
            return r;
        }

        void OnGUI()
        {
            if (game == null) return;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            if (quotes == null) quotes = GetComponent<QuoteDirector>();
            EnsureStyles();
            switch (game.State)
            {
                case GState.Start: DrawStart(); break;
                case GState.Settings: DrawSettings(); break;
                case GState.Playing:
                    DrawHUD(); break;
                case GState.Falling:
                    break;
                case GState.Over: DrawOver(); break;
            }
        }

        void DrawHUD()
        {
            float w = Screen.width, h = Screen.height;
            float scale = UiScale;
            bool showQuote = quotes != null && quotes.IsVisible;
            Rect quoteArea = showQuote ? QuoteRect(quotes.CurrentText, quotes.Opacity) : new Rect();
            // шкала сил
            float bx = 26f * scale, by = 46f * scale, bw = Mathf.Min(w * 0.34f, 400f * scale), bh = 14f * scale;
            var staminaLabel = new GUIStyle(hint)
            {
                font = uiBoldFont,
                fontStyle = FontStyle.Normal,
                fontSize = Mathf.RoundToInt(19f * scale),
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.79f, 0.76f, 0.69f) }
            };
            TrackedLabel(new Rect(bx, by - 33f * scale, bw, 28f * scale), "СИЛЫ", staminaLabel, 2.1f * scale);
            Rect barRect = new Rect(bx, by, bw, bh);
            Rect2(barRect, new Color(0.035f, 0.03f, 0.07f, 0.58f));
            float pct = game.Stamina / GameConfig.StaminaMax;
            Color fill = pct > 0.5f
                ? new Color(0.68f, 0.57f, 0.31f)
                : pct > 0.22f ? new Color(0.88f, 0.66f, 0.27f) : new Color(0.72f, 0.31f, 0.25f);
            Rect2(new Rect(bx, by, bw * pct, bh), fill);

            // высота / рекорд
            var heightLabel = new GUIStyle(body) { font = uiBoldFont, fontStyle = FontStyle.Normal, fontSize = Mathf.RoundToInt(27f * scale) };
            var heightValue = new GUIStyle(body) { font = uiBoldFont, fontStyle = FontStyle.Normal, fontSize = Mathf.RoundToInt(27f * scale) };
            DrawMetric(new Rect(w - 430f * scale, 12f * scale, 410f * scale, 44f * scale),
                "ВЫСОТА", Roman.To(game.Height), heightLabel, heightValue, 2f * scale, 1.5f * scale, 12f * scale, true);
            var right2 = new GUIStyle(hint) { font = uiFont, fontSize = Mathf.RoundToInt(18f * scale), alignment = TextAnchor.UpperRight, normal = { textColor = new Color(0.72f, 0.69f, 0.62f) } };
            TrackedLabel(new Rect(w - 430f * scale, 62f * scale, 410f * scale, 30f * scale), "РЕКОРД  " + Roman.To(game.Best), right2, 1.5f * scale);

            // Все подсказки геймплея раскладываются одним помощником и рисуются одной
            // плашкой — см. LayoutHintPlates/PaintHintPlate.
            var plates = LayoutHintPlates(w, h, scale, MeasureHintLine,
                showQuote ? quoteArea.y : h);
            for (int i = 0; i < plates.Count; i++) PaintHintPlate(plates[i], scale);

            if (EpigraphVisible) DrawStoneQuote(Epigraph, EpigraphOpacity);

            if (showQuote) DrawStoneQuote(quotes.CurrentText, quotes.Opacity);
        }

        /// <summary>
        /// Цвет бейджа защиты от дождя. Состояний у бейджа три, а цветов ДВА, и делятся они
        /// не по состоянию, а по тому, нужно ли от игрока действие.
        ///
        /// «Дождя нет. Отключи защиту…» и «Дождь прошёл. Отключи защиту…» просят РОВНО одно
        /// и то же — снять защиту. Раньше они были покрашены по-разному (красный и янтарный),
        /// и разный цвет у одинаковой просьбы игрока только сбивал. Теперь оба идут золотом
        /// игры — тем же, которым написаны заголовок, «★ НОВЫЙ РЕКОРД ★» и подсказка первого
        /// действия <see cref="CrankStartHint"/>, то есть цветом, который на стойке уже
        /// означает «читай сюда, от тебя нужно действие».
        ///
        /// Красного здесь больше нет: основательница забраковала его живым кадром («странно
        /// она красным смотрится — не очень»). Он и мерился хуже всех — тёмно-красный по
        /// коричневой скале почти не отличался от неё по светлоте, а бейдж висит внизу
        /// экрана, где под ним чаще всего именно скала.
        ///
        /// Бирюза у состояния «ЗАЩИТА ОТ ДОЖДЯ — ВКЛ» осталась: она отличает «всё правильно,
        /// ничего не делай» от «нужно действие» и в палитре игры больше нигде не встречается.
        /// </summary>
        public static Color RainGuardColor(bool needsAction) => needsAction
            ? new Color(1f, 0.8f, 0.2f)          // золото игры: заголовок, рекорд, подсказка крутилки
            : new Color(0.49f, 0.94f, 0.82f);    // бирюза: всё правильно, действия не нужно

        // ─── Единая плашка подсказки геймплея ──────────────────────────────────────────
        //
        // Решение основательницы у живого автомата: «все подсказки к геймплею надо сделать
        // на одинаковых плашках». До этого подсказок было три вида и выглядели они по-разному:
        // подсказка первого действия шла золотом по тёмной плашке с золотой рамкой (на кадрах
        // читалась лучше всех), а баннеры препятствий и бейдж защиты — тонкой обведённой
        // строкой прямо по пейзажу, где их съедали скала, дождь и облака. За эталон взята
        // плашка первого действия; к ней приведены остальные. Геометрия, непрозрачность
        // фона, отступы и трекинг теперь общие — отличается только кегль и цвет текста.
        //
        // Ранний вариант «подложки» основательница забраковала, но браковала она КРАСНЫЙ цвет
        // бейджа (перекрашен в золото, см. RainGuardColor), а не саму плашку: живьём она
        // просит ровно обратное — единый вид.
        //
        // РАМКИ У ПЛАШЕК БОЛЬШЕ НЕТ. Золотую обводку сняла основательница («у текста убрать
        // рамки у текста»): на кадре она превращала каждую подсказку в отдельное «окно»
        // поверх пейзажа, спорила с золотом самого текста и тянула взгляд на края, а не на
        // слова. Остаётся ровно один слой оформления — тёмная подложка под текстом. Снята
        // она у ВСЕХ подсказок разом, поэтому единство набора не пострадало: рисовальщик
        // по-прежнему один (PaintHintPlate), и вернуть рамку выборочно одной подсказке
        // теперь просто негде.
        //
        // Каменной плашки цитат Камю и эпиграфа это не касается: у них свой слой
        // (DrawStoneQuote), и подсказками геймплея они не являются.

        /// <summary>Заливка плашки: тот же ночной тон, что у плашки первого действия.
        /// Единственный слой оформления — рамки у плашки нет.</summary>
        public static readonly Color HintPlateFill = new Color(0.035f, 0.03f, 0.07f, 0.78f);
        /// <summary>Обводка текста: тот же тон, что заливка, но плотнее — отрывает глиф от фона.</summary>
        public static readonly Color HintPlateStroke = new Color(0.035f, 0.03f, 0.07f, 0.96f);
        /// <summary>Цвет баннера препятствия. Тёплый light-cream: на тёмной плашке читается
        /// не хуже золота, но остаётся узнаваемо «не золотым» — золото в игре занято
        /// значением «от тебя нужно действие с органом стойки».</summary>
        public static readonly Color HintPlateBannerText = new Color(1f, 0.95f, 0.82f);
        /// <summary>Цвет подсказки первого действия — золото игры.</summary>
        public static readonly Color HintPlateCrankText = new Color(1f, 0.8f, 0.2f);

        // ─── Имена цветных кнопок — цветом самой кнопки ────────────────────────────────
        //
        // Решение основательницы: «кнопки которые нужно нажимать выделить цветом в тексте».
        // Игрок стоит у стойки, где кнопки физически жёлтая, зелёная и красная (наклейки
        // заказаны). Если слово «жёлтую» в подсказке написано тем же кремовым, что и весь
        // текст, связь «слово на экране ↔ железка под рукой» игрок достраивает сам и не
        // всегда быстро. Покрашенное слово эту связь отдаёт мгновенно.
        //
        // ПОЧЕМУ НА ОТРИСОВКЕ, А НЕ РАЗМЕТКОЙ В ТЕКСТЕ. У GUIStyle есть richText, и соблазн
        // написать «нажми <color=#…>красную кнопку</color>» прямо в строке велик. Так делать
        // нельзя по трём причинам, и каждой хватило бы отдельно:
        //   1. Строки подсказок утверждала основательница, и на них стоят тесты, сверяющие
        //      их посимвольно (HintPlatesTests.HintWording_IsUntouched). Разметка в константе
        //      означала бы, что текст игры больше не равен тому, что она утверждала.
        //   2. Плашка меряет и переносит строку сама (WrapHintText/TrackedTextWidth), а меряет
        //      она ПОСИМВОЛЬНО. Теги попали бы в замер как обычные буквы, и раскладка поехала
        //      бы ровно на их длину.
        //   3. Строки рисуются посимвольно (TrackedLabel), то есть richText до них и не дошёл
        //      бы: теги вывелись бы на экран как текст.
        // Поэтому цвет считается отдельно от текста — по самому тексту (HintTextColors) —
        // и применяется в момент отрисовки. Константы строк не тронуты ни одной буквой.
        //
        // Крутилка, датчики и кнопка меню НЕ красятся: на стойке у них цвета нет, и выдумать
        // им код значило бы обещать игроку несуществующую подсказку. Они идут основным цветом
        // строки, как и раньше.

        /// <summary>Жёлтая кнопка. Лимонный, а не золото игры: золото на плашках уже занято
        /// значением «от тебя нужно действие», и слово-имя органа обязано от него отличаться.</summary>
        public static readonly Color OrganYellow = new Color(1f, 0.89f, 0.23f);
        /// <summary>Зелёная кнопка. Светлая трава: на ночной подложке живая, с бирюзой бейджа
        /// защиты (0.49, 0.94, 0.82) не путается — у той синий канал вдвое выше.</summary>
        public static readonly Color OrganGreen = new Color(0.45f, 0.92f, 0.45f);
        /// <summary>Красная кнопка. Не чистый красный: (1,0,0) на тёмной подложке почти не
        /// светлее её самой (относительная яркость 0.21) и читается как грязное пятно — это
        /// ровно та беда, за которую основательница забраковала красный бейдж защиты. Взят
        /// осветлённый тёплый красный: яркость втрое выше, а красным он остаётся однозначно —
        /// зелёный и синий каналы вдвое с лишним ниже красного.</summary>
        public static readonly Color OrganRed = new Color(1f, 0.40f, 0.33f);

        /// <summary>Основа имени органа: прилагательное-цвет + существительное «кнопка» в
        /// любой падежной форме («жёлтую кнопку», «ЗЕЛЁНАЯ КНОПКА», «красную кнопку»).</summary>
        const string OrganNounStem = "кнопк";

        static readonly string[] OrganStems = { "жёлт", "зелён", "красн" };
        static readonly Color[] OrganStemColors = { OrganYellow, OrganGreen, OrganRed };

        /// <summary>
        /// Цвет каждого символа строки: по умолчанию <paramref name="baseColor"/>, а на имени
        /// цветной кнопки — цвет этой кнопки. Функция чистая и по этому же тексту считается
        /// и в тесте — подсветку нельзя потерять молча.
        ///
        /// Красится ЦЕЛИКОМ словосочетание «прилагательное + кнопка», а не одно прилагательное:
        /// игрок ищет глазами кнопку, и обрывать подсветку на «красную» значит подсвечивать
        /// половину имени органа.
        /// </summary>
        public static Color[] HintTextColors(string text, Color baseColor)
        {
            var colors = new Color[string.IsNullOrEmpty(text) ? 0 : text.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = baseColor;
            if (colors.Length == 0) return colors;

            // Регистр строк разный («жёлтую кнопку» в баннере, «ЗЕЛЁНАЯ КНОПКА» на кнопке
            // экрана), поэтому ищем по нижнему регистру. Для кириллицы ToLowerInvariant
            // длину строки не меняет, так что индексы остаются индексами исходного текста.
            string lower = text.ToLowerInvariant();

            for (int s = 0; s < OrganStems.Length; s++)
            {
                string stem = OrganStems[s];
                int from = 0;
                while (from <= lower.Length - stem.Length)
                {
                    int at = lower.IndexOf(stem, from, System.StringComparison.Ordinal);
                    if (at < 0) break;
                    from = at + stem.Length;

                    if (at > 0 && char.IsLetter(lower[at - 1])) continue;   // середина чужого слова

                    int adjectiveEnd = WordEnd(lower, at);
                    int nounStart = adjectiveEnd;
                    while (nounStart < lower.Length && lower[nounStart] == ' ') nounStart++;
                    if (nounStart == adjectiveEnd) continue;                // слова не разделены
                    if (!StartsWithAt(lower, nounStart, OrganNounStem)) continue;

                    int nounEnd = WordEnd(lower, nounStart);
                    for (int c = at; c < nounEnd; c++) colors[c] = OrganStemColors[s];
                    from = nounEnd;
                }
            }
            return colors;
        }

        static int WordEnd(string text, int start)
        {
            int end = start;
            while (end < text.Length && char.IsLetter(text[end])) end++;
            return end;
        }

        static bool StartsWithAt(string text, int at, string value)
        {
            if (at + value.Length > text.Length) return false;
            for (int i = 0; i < value.Length; i++)
                if (text[at + i] != value[i]) return false;
            return true;
        }

        /// <summary>
        /// Цвета символов, разложенные по строкам уже перенесённой подсказки.
        ///
        /// Считаются по ЦЕЛОМУ тексту, а потом нарезаются по строкам, а не считаются в каждой
        /// строке заново: перенос может разорвать имя органа пополам («…держи жёлтую» /
        /// «кнопку и продолжай…»), и построчный поиск такую половинку уже не узнает —
        /// подсветка потерялась бы ровно на самых длинных подсказках.
        /// </summary>
        public static Color[][] HintLineColors(string text, string[] lines, Color baseColor)
        {
            Color[] all = HintTextColors(text, baseColor);
            var result = new Color[lines.Length][];
            int cursor = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                var lineColors = new Color[lines[i].Length];
                int at = text == null ? -1 : text.IndexOf(lines[i], cursor, System.StringComparison.Ordinal);
                if (at < 0)
                {
                    for (int c = 0; c < lineColors.Length; c++) lineColors[c] = baseColor;
                }
                else
                {
                    System.Array.Copy(all, at, lineColors, 0, lineColors.Length);
                    cursor = at + lineColors.Length;
                }
                result[i] = lineColors;
            }
            return result;
        }

        public static float HintPlatePadX(float scale) => 30f * scale;
        public static float HintPlatePadY(float scale) => 16f * scale;
        public static float HintPlateLineHeight(int fontSize) => fontSize * 1.45f;
        /// <summary>Трекинг привязан к кеглю, а не к масштабу экрана: у плашки первого
        /// действия он исторически 1.8*scale при кегле 30*scale — ровно 6 % кегля. Так
        /// плашки с разным кеглем всё равно читаются одним набором.</summary>
        public static float HintPlateTracking(int fontSize) => fontSize * 0.06f;

        /// <summary>Одна выложенная плашка подсказки: коробка, строки и цвет текста.</summary>
        public readonly struct HintPlate
        {
            public readonly string Id;
            public readonly string Text;
            public readonly string[] Lines;
            public readonly Color TextColor;
            public readonly int FontSize;
            public readonly Rect Box;
            /// <summary>Цвет каждого символа каждой строки: <see cref="TextColor"/> везде, кроме
            /// имён цветных кнопок стойки — там цвет самой кнопки (см. <see cref="HintTextColors"/>).
            /// Лежит в раскладке, а не считается на отрисовке, чтобы подсветку можно было
            /// проверить тестом на том же пути, которым её рисуют.</summary>
            public readonly Color[][] LineColors;

            public HintPlate(string id, string text, string[] lines, Color textColor, int fontSize, Rect box,
                Color[][] lineColors)
            {
                Id = id; Text = text; Lines = lines; TextColor = textColor; FontSize = fontSize; Box = box;
                LineColors = lineColors;
            }
        }

        /// <summary>
        /// Раскладка ВСЕХ подсказок геймплея, которые сейчас на экране: баннер препятствия,
        /// подсказка первого действия и бейдж защиты от дождя. Один список — одна геометрия:
        /// развести их по разным помощникам больше нельзя, не сломав тест
        /// <c>HintPlatesTests</c>.
        ///
        /// Замер текста вынесен в делегат <paramref name="measure"/> (строка + кегль → ширина
        /// с трекингом): рисовать умеет только OnGUI, а проверять раскладку надо и без него.
        ///
        /// <paramref name="quoteTop">верх каменной плашки цитаты (или высота экрана, если
        /// цитаты нет) — бейдж защиты прижимается к нему снизу, чтобы не наехать на цитату.</paramref>
        /// </summary>
        public System.Collections.Generic.List<HintPlate> LayoutHintPlates(
            float w, float h, float scale, System.Func<string, int, float> measure, float quoteTop)
        {
            var plates = new System.Collections.Generic.List<HintPlate>(3);
            float maxWidth = w * 0.92f;

            // Баннер препятствия — у верхнего края, под шкалой СИЛ и счётчиками.
            // Подсказки основательницы — цельные предложения («Пошёл ливень. Включи защиту
            // от дождя: нажми 1 раз красную кнопку и продолжай крутить.»), и плашка обязана
            // расти под строку: не влезло — переносим на вторую, а не режем по краю.
            string bannerText = game != null ? BannerText() : null;
            if (bannerText != null)
                plates.Add(MakeHintPlate("banner", bannerText, HintPlateBannerText,
                    HintFontSize(24f, scale), scale, w * 0.5f, HintBannerTop(scale), false, maxWidth, measure));

            // Подсказка первого действия. С баннером препятствия она не пересекается по
            // построению (CrankHintVisible требует BannerText() == null), но с бейджем
            // защиты — вполне, поэтому живёт в верхней трети, а бейдж у низа.
            if (CrankHintVisible)
                plates.Add(MakeHintPlate("crank", CrankStartHint, HintPlateCrankText,
                    HintFontSize(30f, scale), scale, w * 0.5f, Mathf.Max(150f * scale, h * 0.26f),
                    false, maxWidth, measure));

            // Бейдж защиты от дождя: игрок ОБЯЗАН видеть, включился ли режим (живой фидбек:
            // «нажимаю красную — не понимаю, включилось ли»). Прижат к низу, а при живой
            // цитате Камю — к её верхней кромке.
            //
            // Зеркального бейджа «защита ВЫКЛ» здесь больше нет. Он загорался по условию
            // «дождь идёт, а защита не включена» — то есть ровно тогда, когда наверху уже
            // висит баннер «Пошёл дождь. Включи защиту от дождя…», и дублировал его слово
            // в слово («уже вроде не надо» — основательница).
            if (game != null && game.Careful)
            {
                bool exitGrace = game.RainExitGrace > 0f;
                string guardText = game.CarefulBad
                    ? "Дождя нет. Отключи защиту от дождя: нажми красную кнопку."
                    : exitGrace ? "Дождь прошёл. Отключи защиту от дождя: нажми красную кнопку."
                                : "ЗАЩИТА ОТ ДОЖДЯ — ВКЛ";
                float bottom = quoteTop < h ? quoteTop - 20f * scale : h - 26f * scale;
                plates.Add(MakeHintPlate("guard", guardText, RainGuardColor(game.CarefulBad || exitGrace),
                    HintFontSize(24f, scale), scale, w * 0.5f, bottom, true, maxWidth, measure));
            }

            return plates;
        }

        static int HintFontSize(float baseSize, float scale) =>
            Mathf.Max(18, Mathf.RoundToInt(baseSize * scale));

        /// <summary>Верхняя кромка баннера препятствия. Ниже строки «РЕКОРД …» справа
        /// (62*scale + 30*scale) и шкалы СИЛ слева — плашка стала выше тонкой строки,
        /// и запас нужен реальный.</summary>
        public static float HintBannerTop(float scale) => 110f * scale;

        /// <summary>Зоны HUD, куда плашкам подсказок заезжать нельзя: шкала СИЛ слева
        /// и счётчики высоты/рекорда справа.</summary>
        public static Rect[] HudReservedZones(float w, float scale) => new[]
        {
            new Rect(26f * scale, 13f * scale, Mathf.Min(w * 0.34f, 400f * scale), 47f * scale),
            new Rect(w - 430f * scale, 12f * scale, 410f * scale, 80f * scale)
        };

        static HintPlate MakeHintPlate(string id, string text, Color textColor, int fontSize, float scale,
            float centerX, float edgeY, bool anchorBottom, float maxWidth,
            System.Func<string, int, float> measure)
        {
            float padX = HintPlatePadX(scale), padY = HintPlatePadY(scale);
            string[] lines = WrapHintText(text, fontSize, maxWidth - padX * 2f, measure);

            float textWidth = 0f;
            for (int i = 0; i < lines.Length; i++) textWidth = Mathf.Max(textWidth, measure(lines[i], fontSize));

            float boxW = Mathf.Min(textWidth + padX * 2f, maxWidth);
            float boxH = lines.Length * HintPlateLineHeight(fontSize) + padY * 2f;
            float y = anchorBottom ? edgeY - boxH : edgeY;
            return new HintPlate(id, text, lines, textColor, fontSize,
                new Rect(centerX - boxW * 0.5f, y, boxW, boxH),
                HintLineColors(text, lines, textColor));
        }

        /// <summary>
        /// Перенос строки по словам под ширину плашки. Жадный проход даёт первую строку почти
        /// во всю ширину и куцый хвост, поэтому найденное число строк потом «утрамбовывается»
        /// более узким лимитом — так две строки выходят примерно равной длины.
        /// </summary>
        static string[] WrapHintText(string text, int fontSize, float maxTextWidth,
            System.Func<string, int, float> measure)
        {
            float full = measure(text, fontSize);
            if (full <= maxTextWidth || maxTextWidth <= 0f) return new[] { text };

            string[] greedy = GreedyWrap(text, fontSize, maxTextWidth, measure);
            if (greedy.Length < 2) return greedy;

            float ideal = full / greedy.Length;
            for (float limit = ideal; limit < maxTextWidth; limit += (maxTextWidth - ideal) * 0.1f + 1f)
            {
                string[] balanced = GreedyWrap(text, fontSize, limit, measure);
                if (balanced.Length <= greedy.Length) return balanced;
            }
            return greedy;
        }

        static string[] GreedyWrap(string text, int fontSize, float limit,
            System.Func<string, int, float> measure)
        {
            string[] words = text.Split(' ');
            var lines = new System.Collections.Generic.List<string>();
            string current = string.Empty;
            for (int i = 0; i < words.Length; i++)
            {
                string candidate = current.Length == 0 ? words[i] : current + " " + words[i];
                if (current.Length > 0 && measure(candidate, fontSize) > limit)
                {
                    lines.Add(current);
                    current = words[i];
                }
                else current = candidate;
            }
            if (current.Length > 0) lines.Add(current);
            return lines.Count == 0 ? new[] { text } : lines.ToArray();
        }

        /// <summary>Замер строки ровно тем шрифтом и трекингом, которыми её и нарисуют.</summary>
        float MeasureHintLine(string text, int fontSize) =>
            TrackedTextWidth(text, HintTextStyle(fontSize, Color.white), HintPlateTracking(fontSize));

        GUIStyle HintTextStyle(int fontSize, Color color) => PassiveText(new GUIStyle(hint)
        {
            font = uiBoldFont,
            fontStyle = FontStyle.Normal,
            fontSize = fontSize,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
            normal = { textColor = color }
        });

        /// <summary>
        /// Единственное место, где рисуется оформление плашки подсказки. Оформления ровно
        /// один слой — тёмная подложка <see cref="HintPlateFill"/>; рамки нет (см. блок выше).
        /// Ветвлений по <see cref="HintPlate.Id"/> здесь нет и быть не должно: как только
        /// оформление начнёт зависеть от конкретной подсказки, набор перестанет быть единым.
        /// </summary>
        void PaintHintPlate(HintPlate plate, float scale)
        {
            Rect2(plate.Box, HintPlateFill);

            var style = HintTextStyle(plate.FontSize, plate.TextColor);
            float tracking = HintPlateTracking(plate.FontSize);
            float lineHeight = HintPlateLineHeight(plate.FontSize);
            float padX = HintPlatePadX(scale), padY = HintPlatePadY(scale);
            float stroke = Mathf.Max(0.5f, 0.4f * scale);
            for (int i = 0; i < plate.Lines.Length; i++)
                TrackedOutlinedLabel(
                    new Rect(plate.Box.x + padX, plate.Box.y + padY + i * lineHeight,
                        plate.Box.width - padX * 2f, lineHeight),
                    plate.Lines[i], style, tracking, stroke, HintPlateStroke,
                    plate.LineColors != null && i < plate.LineColors.Length ? plate.LineColors[i] : null);
        }

        /// <summary>
        /// Эпиграф виден только во вступлении забега — пока Сизиф идёт к камню и крутилка
        /// ещё мертва. К концу вступления он уже погашен, поэтому с подсказкой первого
        /// действия (<see cref="CrankHintVisible"/>, требует !IntroActive) они не
        /// пересекаются ни одним кадром.
        /// </summary>
        public bool EpigraphVisible =>
            game != null && game.State == GState.Playing && game.IntroActive &&
            EpigraphOpacity > 0.001f;

        /// <summary>Появление и уход эпиграфа внутри вступления: проявляется сразу,
        /// гаснет к моменту, когда игрок берётся за крутилку.</summary>
        public float EpigraphOpacity
        {
            get
            {
                if (game == null || game.State != GState.Playing || !game.IntroActive) return 0f;
                float p = game.IntroProgress;
                return Mathf.SmoothStep(0f, 1f,
                    Mathf.Min(Mathf.Clamp01(p / 0.12f), Mathf.Clamp01((1f - p) / 0.18f)));
            }
        }

        /// <summary>
        /// Видна ли подсказка первого действия. Поражение игрок проходит НИЧЕГО НЕ ДЕЛАЯ:
        /// падение, скатывание камня, экран проигрыша и вступление нового забега крутилку
        /// не слушают вовсе. Подсказку показываем ровно в тот кадр, когда крутилка снова
        /// оживает (вступление кончилось), и снимаем на первом толчке — чтобы игрок у
        /// стойки не гадал, ждёт от него игра чего-то или нет. Баннер препятствия имеет
        /// приоритет: он занимает ту же роль «что делать сейчас» и сам зовёт крутить.
        /// </summary>
        public bool CrankHintVisible =>
            game != null && game.State == GState.Playing && !game.IntroActive &&
            game.AwaitingFirstPush && BannerText() == null;

        /// <summary>
        /// Баннер «что делать сейчас». Органы здесь названы словами единого словаря стойки
        /// (крутилка · жёлтая кнопка · зелёная кнопка · красная кнопка · кнопка меню) —
        /// ровно так они подписаны на самой стойке физически.
        ///
        /// Крутилка — сознательное исключение: в баннерах она остаётся ГЛАГОЛОМ («крути»,
        /// «НЕ КРУТИ»). Имя органа игрок получает раньше — подсказкой первого действия
        /// <see cref="CrankStartHint"/>, которая загорается в начале КАЖДОГО забега и висит
        /// до первого толчка (со стартового экрана строка органов снята, так что это
        /// единственное место, где крутилка названа по имени). К моменту первого препятствия
        /// «крути» уже однозначно. Вписать «крутилку» в каждый баннер значит повторять имя
        /// в строке, которую читают на ходу за долю секунды.
        /// </summary>
        string BannerText()
        {
            if (game.WindExitGrace > 0f)
                return "ВЕТЕР СТИХ — КРУТИ";

            switch (game.Obstacle)
            {
                case ObKind.Wind:
                    return (game.WindVariant == 0 ? "ВЕТЕР" : game.WindVariant == 1 ? "ПОРЫВЫ" : "ТУРБУЛЕНТНЫЙ ВЕТЕР") +
                        " — НЕ КРУТИ";
                case ObKind.Rain:
                    // «1 раз» прямо в баннере: живой фидбек — игроки ЗАЖИМАЮТ красную кнопку,
                    // хотя это тоггл и достаточно короткого нажатия.
                    //
                    // Сила дождя (морось/дождь/ливень) осталась в первом предложении: действие
                    // у всех трёх вариантов одно, но само явление разное — частицы, звук и
                    // скорость падения у ливня не те, что у мороси, и молча свести их к одному
                    // слову значило бы потерять различие, которое игра честно показывает.
                    // Второе предложение — дословная формулировка основательницы, общая для всех.
                    return (game.RainVariant == 0 ? "Пошла морось." : game.RainVariant == 1 ? "Пошёл дождь." : "Пошёл ливень.") +
                        " Включи защиту от дождя: нажми 1 раз красную кнопку и продолжай крутить.";
                case ObKind.Ice: return game.ActiveIceDistance <= game.VW * 0.28f ? "Камень наехал на лёд. Старайся крутить ровно." : null;
                // «!» был голым значком: на стойке этот орган подписан «ЖЁЛТАЯ КНОПКА»,
                // и связи между значком на экране и кнопкой под рукой игрок не видел.
                case ObKind.Steep: return game.ActiveSteepDistance <= game.VW * 0.30f ? "Крутой склон. Зажми и держи жёлтую кнопку и продолжай крутить." : null;
                default: return null;
            }
        }

        Rect Center(float cw, float ch) => new Rect(Screen.width / 2 - cw / 2, Screen.height / 2 - ch / 2, cw, ch);

        /// <summary>
        /// Стартовый экран стойки. Решение основательницы после плейтеста: экран должен
        /// читаться за пару секунд, а не читаться вовсе («слишком он большой, его никто не
        /// читает»). Поэтому здесь ТОЛЬКО то, без чего игрок не начнёт: заголовок, суть
        /// (толкать камень вверх) и кнопка старта.
        ///
        /// Разбор препятствий (плашка «КАК ПРЕОДОЛЕВАТЬ ПРЕПЯТСТВИЯ», строки лёд/склон/дождь/ветер
        /// и абзац про расход сил) снят целиком: эти объяснения уже есть ПО ХОДУ игры — баннер
        /// препятствия всплывает ровно тогда, когда оно подъезжает (см. BannerText), и работает
        /// лучше предварительного чтения. Нижняя строка органов снята следом — по той же
        /// причине и по её же слову. Эпиграф про богов со стартового экрана тоже ушёл, но не
        /// пропал: он переехал во вступление забега (см. <see cref="Epigraph"/>).
        ///
        /// Мраморная плашка вместе с разбором тоже ушла: её текстура 512x256 рассчитана на
        /// блок в несколько строк, под одну строку она растягивается в ленту и крошит
        /// сколотые углы. Оставшаяся строка идёт светлым текстом по затемнению.
        /// </summary>
        void DrawStart()
        {
            Rect2(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.03f, 0.07f, 0.82f));
            float scale = UiScale;
            float contentW = Mathf.Min(Screen.width * 0.72f, 900f * scale);
            float x = (Screen.width - contentW) * 0.5f;
            // Блок стал коротким — кладём его по центру экрана (с лёгким подъёмом вверх),
            // иначе после выноса плашки он висит в верхней трети над пустотой.
            // 210 = низ кнопки старта: после выноса строки органов кнопка — последний
            // элемент блока, и высоту центрирования надо считать по ней, иначе весь экран
            // уезжает вверх ровно на высоту снятой строки и кнопка повисает над пустотой.
            const float blockH = 210f;
            float top = Mathf.Max(28f * scale, (Screen.height - blockH * scale) * 0.5f - 40f * scale);

            var startTitle = new GUIStyle(title) { fontSize = Mathf.Max(34, Mathf.RoundToInt(44f * scale)) };
            // Суть игры — единственная оставшаяся строка «про что это».
            var pitch = PassiveText(new GUIStyle(body)
            {
                font = uiFont,
                fontSize = Mathf.Max(15, Mathf.RoundToInt(20f * scale)),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.80f, 0.77f, 0.71f, 0.95f) }
            });
            float displayPixelScale = scale >= 1.25f ? 3f : 2f;
            PixelTrackedLabel(new Rect(x, top, contentW, 60f * scale), "БЕСКОНЕЧНЫЙ СИЗИФ", startTitle,
                4.5f * scale, displayPixelScale);
            DrawTitleDivider(Screen.width * 0.5f, top + 64f * scale, Mathf.Min(170f * scale, contentW * 0.3f), scale);
            PassiveLabel(new Rect(x + 40f * scale, top + 92f * scale, contentW - 80f * scale, 40f * scale),
                "Толкай камень вверх по бесконечному склону — так высоко, как хватит сил.", pitch);

            float buttonW = Mathf.Min(contentW * 0.58f, 440f * scale);
            float buttonX = (Screen.width - buttonW) * 0.5f;
            var startButton = new GUIStyle(btn)
            {
                font = uiStrongFont,
                fontSize = Mathf.Max(17, Mathf.RoundToInt(19f * scale)),
                fontStyle = FontStyle.Normal
            };
            if (OrganButton(new Rect(buttonX, top + 160f * scale, buttonW, 50f * scale), StartButtonLabel, startButton)) game.StartGame();

            // Нижняя строка органов («КРУТИЛКА — толкать · ЗЕЛЁНАЯ КНОПКА — начать ·
            // КНОПКА МЕНЮ — выход») снята целиком по решению основательницы. Экран и так
            // читается за пару секунд, а всё, что она перечисляла, игрок получает там, где
            // это нужно: зелёная — на самой кнопке старта, крутилка — подсказкой первого
            // действия в начале забега, кнопка меню — строкой на экране проигрыша.
            // Кнопки настроек на автомате нет: мыши у стойки нет, сложность берётся дефолтная
            // (DifficultySettings.Load) — экран настроек остаётся в коде, но входа в него с экранов нет.

            float authorMargin = 24f * scale;
            float logoSize = 44f * scale;
            float authorWidth = 260f * scale;
            float authorX = Screen.width - authorMargin - authorWidth;
            float authorY = Screen.height - authorMargin - logoSize;
            var authorStyle = PassiveText(new GUIStyle(hint)
            {
                font = uiFont,
                fontSize = Mathf.Max(12, Mathf.RoundToInt(14f * scale)),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.72f, 0.66f, 0.53f, 0.92f) }
            });
            GUI.DrawTexture(new Rect(authorX, authorY, logoSize, logoSize), authorLogo, ScaleMode.ScaleToFit, true);
            PassiveLabel(new Rect(authorX + 52f * scale, authorY, authorWidth - 52f * scale, logoSize),
                "Автор: Иван Меркурьев", authorStyle);
        }

        void DrawSettings()
        {
            Rect2(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.03f, 0.07f, 0.9f));
            float scale = UiScale;
            var s = game.Set;
            var r = Center(560f * scale, 430f * scale);
            var settingsTitle = new GUIStyle(h2) { fontSize = Mathf.RoundToInt(22f * scale) };
            var settingsButton = new GUIStyle(btn) { fontSize = Mathf.RoundToInt(16f * scale) };
            var resetButton = SecondaryButtonStyle(Mathf.RoundToInt(12f * scale));
            GUILayout.BeginArea(r);
            GUILayout.Label("НАСТРОЙКИ СЛОЖНОСТИ", settingsTitle, GUILayout.Height(34f * scale));
            GUILayout.Space(14f * scale);
            s.DrainMul = Slider("Расход сил", s.DrainMul, 0.5f, 2f, "×" + s.DrainMul.ToString("0.0"), scale);
            s.FreqMul = Slider("Частота явлений", s.FreqMul, 0.5f, 2f, "×" + s.FreqMul.ToString("0.0"), scale);
            s.WindDurMul = Slider("Длительность ветра", s.WindDurMul, 0.5f, 2f, "×" + s.WindDurMul.ToString("0.0"), scale);
            s.RainProb = Slider("Вероятность дождя", s.RainProb, 0f, 3f, RainLabel(s.RainProb), scale);
            s.SteepMul = Slider("Крутизна склонов", s.SteepMul, 0.5f, 2f, "×" + s.SteepMul.ToString("0.0"), scale);
            GUILayout.Space(12f * scale);
            if (GUILayout.Button("НАЗАД", settingsButton, GUILayout.Height(38f * scale))) { s.Save(); game.CloseSettings(); }
            GUILayout.Space(7f * scale);
            if (GUILayout.Button("СБРОС", resetButton, GUILayout.Height(26f * scale))) { s.Reset(); s.Save(); }
            GUILayout.EndArea();
        }

        float Slider(string name, float val, float lo, float hi, string valText, float scale)
        {
            var labelStyle = PassiveText(new GUIStyle(body) { fontSize = Mathf.RoundToInt(14f * scale) });
            var valueStyle = PassiveText(new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.8f, 0.2f) }
            });
            var sliderStyle = new GUIStyle(GUI.skin.horizontalSlider) { fixedHeight = 8f * scale };
            var thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                fixedWidth = 18f * scale,
                fixedHeight = 22f * scale
            };
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, labelStyle, GUILayout.Width(220f * scale), GUILayout.Height(24f * scale));
            GUILayout.Label(valText, valueStyle, GUILayout.Width(90f * scale), GUILayout.Height(24f * scale));
            GUILayout.EndHorizontal();
            float result = GUILayout.HorizontalSlider(val, lo, hi, sliderStyle, thumbStyle, GUILayout.Height(24f * scale));
            GUILayout.Space(5f * scale);
            return result;
        }

        static string RainLabel(float v) => v == 0 ? "нет" : v < 0.75f ? "низкая" : v <= 1.25f ? "обычная" : v <= 2f ? "высокая" : "очень высокая";

        void DrawOver()
        {
            Rect2(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.03f, 0.07f, 0.82f));
            float scale = UiScale;
            float width = Mathf.Min(480f * scale, Screen.width * 0.62f);
            btn.fontSize = Mathf.Max(17, Mathf.RoundToInt(20f * scale));
            float x = (Screen.width - width) * 0.5f;
            float titleWidth = Mathf.Min(760f * scale, Screen.width * 0.90f);
            float titleX = (Screen.width - titleWidth) * 0.5f;
            float top = Mathf.Max(18f, Screen.height * 0.08f);
            var overTitle = new GUIStyle(h2)
            {
                font = displayFont,
                fontSize = Mathf.Max(30, Mathf.RoundToInt(34f * scale)),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                normal = { textColor = title.normal.textColor }
            };
            var overReason = new GUIStyle(hint)
            {
                font = uiFont,
                fontSize = Mathf.Max(15, Mathf.RoundToInt(18f * scale)),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.76f, 0.73f, 0.68f) }
            };
            var overHeightLabel = new GUIStyle(body)
            {
                font = displayFont,
                fontSize = Mathf.Max(27, Mathf.RoundToInt(30f * scale)),
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(1f, 0.8f, 0.2f) }
            };
            var overHeightValue = new GUIStyle(body)
            {
                font = displayFont,
                fontSize = Mathf.Max(27, Mathf.RoundToInt(30f * scale)),
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(1f, 0.8f, 0.2f) }
            };
            var overRecord = new GUIStyle(hint)
            {
                font = uiFont,
                fontSize = Mathf.RoundToInt(17f * scale),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.72f, 0.69f, 0.62f) }
            };

            float displayPixelScale = scale >= 1.25f ? 3f : 2f;
            PixelPassiveLabel(new Rect(titleX, top, titleWidth, 72f * scale),
                game.FallReason == "slip" ? "СИЗИФ ПОСКОЛЬЗНУЛСЯ" : "СИЛЫ ИССЯКЛИ", overTitle,
                displayPixelScale);
            DrawTitleDivider(Screen.width * 0.5f, top + 64f * scale,
                Mathf.Min(170f * scale, titleWidth * 0.30f), scale);
            PassiveLabel(new Rect(titleX, top + 78f * scale, titleWidth, 42f * scale),
                game.FallReason == "slip" ? "Камень вырвался и покатился вниз." : "Руки опустились — камень скатился к подножию.", overReason);
            PixelDrawMetric(new Rect(x, top + 126f * scale, width, 50f * scale),
                "ВЫСОТА", Roman.To(game.Height), overHeightLabel, overHeightValue,
                2f * scale, 2f * scale, 10f * scale, false, displayPixelScale);
            TrackedLabel(new Rect(x, top + 180f * scale, width, 30f * scale), "РЕКОРД  " + Roman.To(game.Best), overRecord, 1.5f * scale);
            if (game.IsRecordScreen)
                PassiveLabel(new Rect(x, top + 208f * scale, width, 30f * scale), "★ НОВЫЙ РЕКОРД ★",
                    new GUIStyle(overRecord) { normal = { textColor = new Color(1f, 0.8f, 0.2f) } });

            Rect defeatQuoteRect = QuoteRect(DefeatQuote, 1f);
            defeatQuoteRect.y = top + 236f * scale;
            DrawStoneQuoteAt(defeatQuoteRect, DefeatQuote, 1f);

            float actionsY = defeatQuoteRect.yMax + 18f * scale;
            if (OrganButton(new Rect(x, actionsY, width, 52f * scale), RestartButtonLabel, btn)) game.StartGame();
            // Вторичной кнопки «В МЕНЮ» на автомате нет: указателя у стойки нет, а выход
            // с экрана — физическая кнопка меню (контракт автомата), она же возвращает в лаунчер.
            PassiveLabel(new Rect(x, actionsY + 62f * scale, width, 30f * scale), "КНОПКА МЕНЮ — выход", overRecord);
        }
    }
}
