using BepInEx.Configuration;

namespace SilksongManager
{
    /// <summary>
    /// Configuration settings for the plugin.
    /// </summary>
    public class PluginConfig
    {
        private readonly ConfigFile _config;

        /// <summary>
        /// Exposes the raw ConfigFile for other modules.
        /// </summary>
        public ConfigFile ConfigFile => _config;

        // Config entries with backing
        private ConfigEntry<bool> _enableHotkeys;
        private ConfigEntry<bool> _showDebugInfo;
        private ConfigEntry<bool> _enableLogging;

        // General Settings - now with public setters
        public bool EnableHotkeys
        {
            get => _enableHotkeys?.Value ?? true;
            set { if (_enableHotkeys != null) _enableHotkeys.Value = value; }
        }

        public bool ShowDebugInfo
        {
            get => _showDebugInfo?.Value ?? false;
            set { if (_showDebugInfo != null) _showDebugInfo.Value = value; }
        }

        public bool EnableLogging
        {
            get => _enableLogging?.Value ?? true;
            set { if (_enableLogging != null) _enableLogging.Value = value; }
        }

        // Debug Menu Settings (read-only)
        public float MenuWidth { get; private set; }
        public float MenuHeight { get; private set; }
        public int FontSize { get; private set; }

        public PluginConfig(ConfigFile config)
        {
            _config = config;
            LoadConfig();
        }

        private void LoadConfig()
        {
            // General Settings
            _enableHotkeys = _config.Bind(
                "General",
                "EnableHotkeys",
                true,
                "Enable keyboard hotkeys for quick actions"
            );

            _showDebugInfo = _config.Bind(
                "General",
                "ShowDebugInfo",
                false,
                "Show debug information on screen"
            );

            _enableLogging = _config.Bind(
                "General",
                "EnableLogging",
                true,
                "Enable logging to BepInEx console"
            );

            // Debug Menu Settings
            MenuWidth = _config.Bind(
                "DebugMenu",
                "MenuWidth",
                400f,
                "Width of the debug menu window"
            ).Value;

            MenuHeight = _config.Bind(
                "DebugMenu",
                "MenuHeight",
                600f,
                "Height of the debug menu window"
            ).Value;

            FontSize = _config.Bind(
                "DebugMenu",
                "FontSize",
                14,
                "Font size for debug menu text"
            ).Value;
        }
    }
}

