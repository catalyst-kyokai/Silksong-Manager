using BepInEx.Configuration;
using UnityEngine;

namespace SilksongManager.DebugMenu
{
    /// <summary>
    /// Configuration for the debug menu.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class DebugMenuConfig
    {
        private static ConfigEntry<float> _backgroundOpacity;
        private static ConfigEntry<float> _fullMenuOpacity;
        private static ConfigEntry<OpacityMode> _opacityMode;
        private static ConfigEntry<float> _mainWindowX;
        private static ConfigEntry<float> _mainWindowY;
        
        private static bool _initialized = false;
        
        /// <summary>
        /// Opacity mode determines what gets transparency applied.
        /// </summary>
        public enum OpacityMode
        {
            BackgroundOnly,
            FullMenu
        }
        
        /// <summary>
        /// Initialize config entries.
        /// </summary>
        public static void Initialize(ConfigFile config)
        {
            if (_initialized) return;
            
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
            
            _mainWindowX = config.Bind(
                "DebugMenu.Windows",
                "MainWindowX",
                20f,
                "X position of main window"
            );
            
            _mainWindowY = config.Bind(
                "DebugMenu.Windows",
                "MainWindowY",
                20f,
                "Y position of main window"
            );
            
            _initialized = true;
        }
        
        // Accessors
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
        
        public static Vector2 MainWindowPosition
        {
            get => new Vector2(_mainWindowX?.Value ?? 20f, _mainWindowY?.Value ?? 20f);
            set
            {
                if (_mainWindowX != null) _mainWindowX.Value = value.x;
                if (_mainWindowY != null) _mainWindowY.Value = value.y;
            }
        }
        
        /// <summary>
        /// Get the effective GUI.color alpha based on opacity mode.
        /// </summary>
        public static float GetEffectiveAlpha()
        {
            return CurrentOpacityMode == OpacityMode.FullMenu ? FullMenuOpacity : 1f;
        }
        
        /// <summary>
        /// Get the effective background alpha.
        /// </summary>
        public static float GetBackgroundAlpha()
        {
            return BackgroundOpacity;
        }
    }
}
