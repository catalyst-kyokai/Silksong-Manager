using BepInEx.Configuration;

namespace SilksongManager
{
    /// <summary>
    /// Configuration settings for the plugin.
    /// </summary>
    public class PluginConfig
    {
        private readonly ConfigFile _config;

        // General Settings
        public bool EnableHotkeys { get; private set; }
        public bool ShowDebugInfo { get; private set; }
        public bool EnableLogging { get; private set; }

        // Debug Menu Settings
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
            EnableHotkeys = _config.Bind(
                "General",
                "EnableHotkeys",
                true,
                "Enable keyboard hotkeys for quick actions"
            ).Value;

            ShowDebugInfo = _config.Bind(
                "General",
                "ShowDebugInfo",
                false,
                "Show debug information on screen"
            ).Value;

            EnableLogging = _config.Bind(
                "General",
                "EnableLogging",
                true,
                "Enable logging to BepInEx console"
            ).Value;

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
