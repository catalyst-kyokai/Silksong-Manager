using BepInEx.Configuration;
using UnityEngine;
using System.Collections.Generic;

namespace SilksongManager.DebugMenu
{
    /// <summary>
    /// Configuration for the debug menu with window state persistence.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class DebugMenuConfig
    {
        #region Private Fields

        /// <summary>Reference to config file.</summary>
        private static ConfigFile _config;
        /// <summary>Background opacity setting.</summary>
        private static ConfigEntry<float> _backgroundOpacity;
        /// <summary>Full menu opacity setting.</summary>
        private static ConfigEntry<float> _fullMenuOpacity;
        /// <summary>Opacity mode setting.</summary>
        private static ConfigEntry<OpacityMode> _opacityMode;
        /// <summary>Pause game on menu setting.</summary>
        private static ConfigEntry<bool> _pauseGameOnMenu;

        /// <summary>Window state storage dictionary.</summary>
        private static Dictionary<int, ConfigEntry<string>> _windowStates = new Dictionary<int, ConfigEntry<string>>();

        /// <summary>Whether config has been initialized.</summary>
        private static bool _initialized = false;

        #endregion

        #region Enums

        public enum OpacityMode
        {
            BackgroundOnly,
            FullMenu
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the debug menu configuration.
        /// </summary>
        /// <param name="config">BepInEx configuration file.</param>
        public static void Initialize(ConfigFile config)
        {
            if (_initialized) return;
            _config = config;

            _backgroundOpacity = config.Bind(
                "DebugMenu",
                "BackgroundOpacity",
                0.9f,
                new ConfigDescription("Opacity of window backgrounds (0-1)", new AcceptableValueRange<float>(0f, 1f))
            );

            _fullMenuOpacity = config.Bind(
                "DebugMenu",
                "FullMenuOpacity",
                1f,
                new ConfigDescription("Opacity of entire menu including text (0-1)", new AcceptableValueRange<float>(0.3f, 1f))
            );

            _opacityMode = config.Bind(
                "DebugMenu",
                "OpacityMode",
                OpacityMode.BackgroundOnly,
                "Whether transparency applies to background only or entire menu"
            );

            _pauseGameOnMenu = config.Bind(
                "DebugMenu",
                "PauseGameOnMenu",
                false,
                "Pause the game when debug menu is opened"
            );

            _initialized = true;
        }

        #endregion

        #region Public Properties
        public static float BackgroundOpacity
        {
            get => _backgroundOpacity?.Value ?? 0.9f;
            set { if (_backgroundOpacity != null) _backgroundOpacity.Value = value; }
        }

        public static float FullMenuOpacity
        {
            get => _fullMenuOpacity?.Value ?? 1f;
            set { if (_fullMenuOpacity != null) _fullMenuOpacity.Value = value; }
        }

        public static OpacityMode CurrentOpacityMode
        {
            get => _opacityMode?.Value ?? OpacityMode.BackgroundOnly;
            set { if (_opacityMode != null) _opacityMode.Value = value; }
        }

        public static bool PauseGameOnMenu
        {
            get => _pauseGameOnMenu?.Value ?? false;
            set { if (_pauseGameOnMenu != null) _pauseGameOnMenu.Value = value; }
        }

        public static Vector2 MainWindowPosition
        {
            get => GetWindowState(10001).Position;
            set => SaveWindowPosition(10001, value);
        }

        public static float GetEffectiveAlpha()
        {
            return CurrentOpacityMode == OpacityMode.FullMenu ? FullMenuOpacity : 1f;
        }

        public static float GetBackgroundAlpha()
        {
            return BackgroundOpacity;
        }

        #endregion

        #region Window State Persistence

        /// <summary>
        /// Window state stored as "x,y,w,h,visible"
        /// </summary>
        public struct WindowState
        {
            public Vector2 Position;
            public Vector2 Size;
            public bool IsVisible;

            public override string ToString()
            {
                // Use InvariantCulture to avoid issues with decimal separator
                return string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0:F0},{1:F0},{2:F0},{3:F0},{4}",
                    Position.x, Position.y, Size.x, Size.y, IsVisible ? 1 : 0
                );
            }

            public static WindowState Parse(string s, Vector2 defaultPos, Vector2 defaultSize)
            {
                var state = new WindowState
                {
                    Position = defaultPos,
                    Size = defaultSize,
                    IsVisible = false
                };

                if (string.IsNullOrEmpty(s)) return state;

                var parts = s.Split(',');
                if (parts.Length >= 5)
                {
                    var culture = System.Globalization.CultureInfo.InvariantCulture;
                    if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, culture, out float x)) state.Position.x = x;
                    if (float.TryParse(parts[1], System.Globalization.NumberStyles.Float, culture, out float y)) state.Position.y = y;
                    if (float.TryParse(parts[2], System.Globalization.NumberStyles.Float, culture, out float w)) state.Size.x = Mathf.Max(200, w);
                    if (float.TryParse(parts[3], System.Globalization.NumberStyles.Float, culture, out float h)) state.Size.y = Mathf.Max(150, h);
                    if (int.TryParse(parts[4], out int v)) state.IsVisible = v == 1;
                }

                return state;
            }
        }

        public static WindowState GetWindowState(int windowId)
        {
            return GetWindowState(windowId, new Vector2(20, 20), new Vector2(280, 300));
        }

        public static WindowState GetWindowState(int windowId, Vector2 defaultPos, Vector2 defaultSize)
        {
            if (_config == null) return new WindowState { Position = defaultPos, Size = defaultSize, IsVisible = false };

            if (!_windowStates.TryGetValue(windowId, out var entry))
            {
                entry = _config.Bind(
                    "DebugMenu.WindowStates",
                    $"Window_{windowId}",
                    "",
                    $"State for window {windowId}: x,y,width,height,visible"
                );
                _windowStates[windowId] = entry;
            }

            return WindowState.Parse(entry.Value, defaultPos, defaultSize);
        }

        public static void SaveWindowState(int windowId, Rect rect, bool isVisible)
        {
            if (_config == null) return;

            if (!_windowStates.TryGetValue(windowId, out var entry))
            {
                entry = _config.Bind(
                    "DebugMenu.WindowStates",
                    $"Window_{windowId}",
                    "",
                    $"State for window {windowId}: x,y,width,height,visible"
                );
                _windowStates[windowId] = entry;
            }

            var state = new WindowState
            {
                Position = new Vector2(rect.x, rect.y),
                Size = new Vector2(rect.width, rect.height),
                IsVisible = isVisible
            };

            entry.Value = state.ToString();
        }

        public static void SaveWindowPosition(int windowId, Vector2 pos)
        {
            var state = GetWindowState(windowId);
            state.Position = pos;

            if (_windowStates.TryGetValue(windowId, out var entry))
            {
                entry.Value = state.ToString();
            }
        }

        #endregion
    }
}

