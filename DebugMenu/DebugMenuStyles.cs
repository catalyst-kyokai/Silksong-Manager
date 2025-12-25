using UnityEngine;

namespace SilksongManager.DebugMenu
{
    /// <summary>
    /// Modern dark/red/gray theme styles for the debug menu.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class DebugMenuStyles
    {
        #region Colors

        // Primary colors
        public static readonly Color BackgroundDark = new Color32(26, 26, 26, 255);      // #1a1a1a
        public static readonly Color BackgroundMedium = new Color32(35, 35, 35, 255);    // #232323
        public static readonly Color BackgroundLight = new Color32(50, 50, 50, 255);     // #323232

        // Accent colors
        public static readonly Color AccentRed = new Color32(192, 57, 43, 255);          // #c0392b
        public static readonly Color AccentRedHover = new Color32(231, 76, 60, 255);     // #e74c3c
        public static readonly Color AccentRedActive = new Color32(155, 45, 35, 255);    // #9b2d23

        // Text colors
        public static readonly Color TextLight = new Color32(236, 240, 241, 255);        // #ecf0f1
        public static readonly Color TextMuted = new Color32(127, 140, 141, 255);        // #7f8c8d
        public static readonly Color TextDisabled = new Color32(100, 100, 100, 255);     // #646464

        // Border/Separator
        public static readonly Color BorderGray = new Color32(52, 73, 94, 255);          // #34495e
        public static readonly Color SeparatorDark = new Color32(40, 40, 40, 255);       // #282828

        // Status colors
        public static readonly Color StatusOn = new Color32(39, 174, 96, 255);           // #27ae60
        public static readonly Color StatusOff = new Color32(192, 57, 43, 255);          // #c0392b
        public static readonly Color StatusWarning = new Color32(241, 196, 15, 255);     // #f1c40f

        #endregion

        #region Textures

        private static Texture2D _backgroundTex;
        private static Texture2D _buttonNormalTex;
        private static Texture2D _buttonHoverTex;
        private static Texture2D _buttonActiveTex;
        private static Texture2D _windowTex;
        private static Texture2D _headerTex;
        private static Texture2D _sliderBgTex;
        private static Texture2D _sliderFillTex;

        public static Texture2D BackgroundTex => _backgroundTex ??= MakeTexture(2, 2, BackgroundDark);
        public static Texture2D ButtonNormalTex => _buttonNormalTex ??= MakeTexture(2, 2, BackgroundLight);
        public static Texture2D ButtonHoverTex => _buttonHoverTex ??= MakeTexture(2, 2, AccentRed);
        public static Texture2D ButtonActiveTex => _buttonActiveTex ??= MakeTexture(2, 2, AccentRedActive);
        public static Texture2D WindowTex => _windowTex ??= MakeTexture(2, 2, BackgroundMedium);
        public static Texture2D HeaderTex => _headerTex ??= MakeTexture(2, 2, AccentRed);
        public static Texture2D SliderBgTex => _sliderBgTex ??= MakeTexture(2, 2, BackgroundDark);
        public static Texture2D SliderFillTex => _sliderFillTex ??= MakeTexture(2, 2, AccentRed);

        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            var tex = new Texture2D(width, height);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        #endregion

        #region GUIStyles

        private static GUIStyle _windowStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _labelBoldStyle;
        private static GUIStyle _labelCenteredStyle;
        private static GUIStyle _labelSmallStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _buttonSmallStyle;
        private static GUIStyle _toggleStyle;
        private static GUIStyle _toggleOnStyle;
        private static GUIStyle _textFieldStyle;
        private static GUIStyle _boxStyle;
        private static GUIStyle _keybindStyle;
        private static GUIStyle _keybindListeningStyle;
        private static GUIStyle _statusOnStyle;
        private static GUIStyle _statusOffStyle;
        private static GUIStyle _closeButtonStyle;
        private static GUIStyle _sectionStyle;

        private static bool _stylesInitialized = false;

        /// <summary>
        /// Initialize all styles. Call this in OnGUI before using styles.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_stylesInitialized) return;
            InitializeStyles();
            _stylesInitialized = true;
        }

        private static void InitializeStyles()
        {
            // Window style
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(8, 8, 24, 8),
                border = new RectOffset(4, 4, 4, 4),
                contentOffset = Vector2.zero
            };
            _windowStyle.normal.background = WindowTex;
            _windowStyle.normal.textColor = TextLight;
            _windowStyle.onNormal.background = WindowTex;
            _windowStyle.onNormal.textColor = TextLight;

            // Header style (for window titles)
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 4, 4)
            };
            _headerStyle.normal.textColor = TextLight;

            // Label styles
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true
            };
            _labelStyle.normal.textColor = TextLight;

            _labelBoldStyle = new GUIStyle(_labelStyle)
            {
                fontStyle = FontStyle.Bold
            };

            _labelCenteredStyle = new GUIStyle(_labelStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            _labelSmallStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 11
            };

            // Button style
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(12, 12, 6, 6),
                margin = new RectOffset(2, 2, 2, 2)
            };
            _buttonStyle.normal.background = ButtonNormalTex;
            _buttonStyle.normal.textColor = TextLight;
            _buttonStyle.hover.background = ButtonHoverTex;
            _buttonStyle.hover.textColor = TextLight;
            _buttonStyle.active.background = ButtonActiveTex;
            _buttonStyle.active.textColor = TextLight;
            _buttonStyle.focused = _buttonStyle.normal;

            // Small button
            _buttonSmallStyle = new GUIStyle(_buttonStyle)
            {
                fontSize = 11,
                padding = new RectOffset(8, 8, 4, 4)
            };

            // Toggle style (off)
            _toggleStyle = new GUIStyle(_buttonStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            // Toggle style (on)
            _toggleOnStyle = new GUIStyle(_toggleStyle);
            _toggleOnStyle.normal.background = ButtonHoverTex;
            _toggleOnStyle.normal.textColor = TextLight;

            // TextField
            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 13,
                padding = new RectOffset(6, 6, 4, 4)
            };
            _textFieldStyle.normal.background = BackgroundTex;
            _textFieldStyle.normal.textColor = TextLight;
            _textFieldStyle.focused.background = ButtonNormalTex;
            _textFieldStyle.focused.textColor = TextLight;

            // Box style (for groups)
            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };
            _boxStyle.normal.background = BackgroundTex;

            // Keybind button
            _keybindStyle = new GUIStyle(_buttonSmallStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = 80
            };

            // Keybind listening (waiting for input)
            _keybindListeningStyle = new GUIStyle(_keybindStyle);
            _keybindListeningStyle.normal.background = ButtonHoverTex;
            _keybindListeningStyle.normal.textColor = TextLight;

            // Status indicators
            _statusOnStyle = new GUIStyle(_labelBoldStyle);
            _statusOnStyle.normal.textColor = StatusOn;

            _statusOffStyle = new GUIStyle(_labelBoldStyle);
            _statusOffStyle.normal.textColor = StatusOff;

            // Close button
            _closeButtonStyle = new GUIStyle(_buttonSmallStyle)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedWidth = 24,
                fixedHeight = 24,
                padding = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleCenter
            };

            // Section header
            _sectionStyle = new GUIStyle(_labelBoldStyle)
            {
                fontSize = 14,
                padding = new RectOffset(4, 4, 8, 4)
            };
            _sectionStyle.normal.textColor = AccentRed;
        }

        // Public accessors
        public static GUIStyle Window => _windowStyle;
        public static GUIStyle Header => _headerStyle;
        public static GUIStyle Label => _labelStyle;
        public static GUIStyle LabelSmall => _labelSmallStyle;
        public static GUIStyle LabelBold => _labelBoldStyle;
        public static GUIStyle LabelCentered => _labelCenteredStyle;
        public static GUIStyle Button => _buttonStyle;
        public static GUIStyle ButtonSmall => _buttonSmallStyle;
        public static GUIStyle Toggle => _toggleStyle;
        public static GUIStyle ToggleOn => _toggleOnStyle;
        public static GUIStyle TextField => _textFieldStyle;
        public static GUIStyle Box => _boxStyle;
        public static GUIStyle Keybind => _keybindStyle;
        public static GUIStyle KeybindListening => _keybindListeningStyle;
        public static GUIStyle StatusOnStyle => _statusOnStyle;
        public static GUIStyle StatusOffStyle => _statusOffStyle;
        public static GUIStyle CloseButton => _closeButtonStyle;
        public static GUIStyle Section => _sectionStyle;

        #endregion

        #region Helpers

        /// <summary>
        /// Draw a horizontal separator line.
        /// </summary>
        public static void DrawSeparator()
        {
            GUILayout.Space(4);
            var rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorDrawLine(rect, SeparatorDark);
            GUILayout.Space(4);
        }

        private static void EditorDrawLine(Rect rect, Color color)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        /// <summary>
        /// Draw a section header with accent color.
        /// </summary>
        public static void DrawSectionHeader(string title)
        {
            GUILayout.Space(8);
            GUILayout.Label(title, Section);
            DrawSeparator();
        }

        /// <summary>
        /// Draw a status indicator (ON/OFF).
        /// </summary>
        public static void DrawStatus(string label, bool isOn)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, Label, GUILayout.Width(120));
            GUILayout.Label(isOn ? "ON" : "OFF", isOn ? StatusOnStyle : StatusOffStyle);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw a toggle button that returns true when clicked.
        /// </summary>
        public static bool DrawToggleButton(string label, bool isOn, float width = 0)
        {
            var style = isOn ? ToggleOn : Toggle;
            var options = width > 0 ? new[] { GUILayout.Width(width) } : new GUILayoutOption[0];
            return GUILayout.Button(label, style, options);
        }

        /// <summary>
        /// Draw a keybind display/edit button.
        /// </summary>
        public static bool DrawKeybindButton(KeyCode key, bool isListening)
        {
            var label = isListening ? "..." : KeyCodeToString(key);
            var style = isListening ? KeybindListening : Keybind;
            return GUILayout.Button(label, style);
        }

        /// <summary>
        /// Convert KeyCode to short display string.
        /// </summary>
        public static string KeyCodeToString(KeyCode key)
        {
            if (key == KeyCode.None) return "---";

            return key switch
            {
                KeyCode.Alpha0 => "0",
                KeyCode.Alpha1 => "1",
                KeyCode.Alpha2 => "2",
                KeyCode.Alpha3 => "3",
                KeyCode.Alpha4 => "4",
                KeyCode.Alpha5 => "5",
                KeyCode.Alpha6 => "6",
                KeyCode.Alpha7 => "7",
                KeyCode.Alpha8 => "8",
                KeyCode.Alpha9 => "9",
                KeyCode.Keypad0 => "Num0",
                KeyCode.Keypad1 => "Num1",
                KeyCode.Keypad2 => "Num2",
                KeyCode.Keypad3 => "Num3",
                KeyCode.Keypad4 => "Num4",
                KeyCode.Keypad5 => "Num5",
                KeyCode.Keypad6 => "Num6",
                KeyCode.Keypad7 => "Num7",
                KeyCode.Keypad8 => "Num8",
                KeyCode.Keypad9 => "Num9",
                KeyCode.LeftShift => "LShift",
                KeyCode.RightShift => "RShift",
                KeyCode.LeftControl => "LCtrl",
                KeyCode.RightControl => "RCtrl",
                KeyCode.LeftAlt => "LAlt",
                KeyCode.RightAlt => "RAlt",
                KeyCode.Equals => "=",
                KeyCode.Minus => "-",
                _ => key.ToString()
            };
        }

        /// <summary>
        /// Draw a dropdown selector button that shows current selection.
        /// Returns true and sets newIndex if selection changed.
        /// </summary>
        public static bool DrawDropdown(string[] options, int selectedIndex, out int newIndex, bool isExpanded, float width = 0)
        {
            newIndex = selectedIndex;
            var options2 = width > 0 ? new[] { GUILayout.Width(width) } : new GUILayoutOption[0];

            string displayText = selectedIndex >= 0 && selectedIndex < options.Length
                ? options[selectedIndex] + " ▼"
                : "Select... ▼";

            if (GUILayout.Button(displayText, ButtonSmall, options2))
            {
                return true; // Signal to toggle expanded state
            }
            return false;
        }

        /// <summary>
        /// Draw the dropdown list items. Call this when dropdown is expanded.
        /// Returns true if an item was selected.
        /// </summary>
        public static bool DrawDropdownList(string[] options, int selectedIndex, out int newIndex, float width = 0, int maxVisible = 8)
        {
            newIndex = selectedIndex;
            var listOptions = width > 0 ? new[] { GUILayout.Width(width) } : new GUILayoutOption[0];

            int displayCount = Mathf.Min(options.Length, maxVisible);

            for (int i = 0; i < options.Length && i < maxVisible; i++)
            {
                bool isSelected = i == selectedIndex;
                var style = isSelected ? ToggleOn : ButtonSmall;

                if (GUILayout.Button(options[i], style, listOptions))
                {
                    newIndex = i;
                    return true;
                }
            }

            if (options.Length > maxVisible)
            {
                GUILayout.Label($"... +{options.Length - maxVisible} more", LabelSmall);
            }

            return false;
        }

        #endregion
    }
}
