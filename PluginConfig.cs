using BepInEx.Configuration;

namespace SilksongManager
{
    /// <summary>
    /// Configuration settings for the Silksong Manager plugin.
    /// Manages all persistent settings through BepInEx configuration system.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class PluginConfig
    {
        #region Private Fields

        /// <summary>
        /// Reference to the BepInEx configuration file.
        /// </summary>
        private readonly ConfigFile _config;

        /// <summary>
        /// Configuration entry for enabling hotkeys.
        /// </summary>
        private ConfigEntry<bool> _enableHotkeys;

        /// <summary>
        /// Configuration entry for showing debug info overlay.
        /// </summary>
        private ConfigEntry<bool> _showDebugInfo;

        /// <summary>
        /// Configuration entry for enabling logging.
        /// </summary>
        private ConfigEntry<bool> _enableLogging;

        #endregion

        #region Public Properties

        /// <summary>
        /// Exposes the raw ConfigFile for use by other modules.
        /// </summary>
        public ConfigFile ConfigFile => _config;

        /// <summary>
        /// Gets or sets whether keyboard hotkeys are enabled for quick actions.
        /// </summary>
        public bool EnableHotkeys
        {
            get => _enableHotkeys?.Value ?? true;
            set { if (_enableHotkeys != null) _enableHotkeys.Value = value; }
        }

        /// <summary>
        /// Gets or sets whether to show debug information overlay on screen.
        /// </summary>
        public bool ShowDebugInfo
        {
            get => _showDebugInfo?.Value ?? false;
            set { if (_showDebugInfo != null) _showDebugInfo.Value = value; }
        }

        /// <summary>
        /// Gets or sets whether logging to BepInEx console is enabled.
        /// </summary>
        public bool EnableLogging
        {
            get => _enableLogging?.Value ?? true;
            set { if (_enableLogging != null) _enableLogging.Value = value; }
        }

        /// <summary>
        /// Gets the configured width of the debug menu window.
        /// </summary>
        public float MenuWidth { get; private set; }

        /// <summary>
        /// Gets the configured height of the debug menu window.
        /// </summary>
        public float MenuHeight { get; private set; }

        /// <summary>
        /// Gets the configured font size for debug menu text.
        /// </summary>
        public int FontSize { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of PluginConfig with the given configuration file.
        /// </summary>
        /// <param name="config">The BepInEx configuration file.</param>
        public PluginConfig(ConfigFile config)
        {
            _config = config;
            LoadConfig();
        }

        #endregion

        #region Configuration Loading

        /// <summary>
        /// Loads all configuration entries from the configuration file.
        /// </summary>
        private void LoadConfig()
        {
            LoadGeneralSettings();
            LoadDebugMenuSettings();
        }

        /// <summary>
        /// Loads general plugin settings.
        /// </summary>
        private void LoadGeneralSettings()
        {
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
        }

        /// <summary>
        /// Loads debug menu appearance settings.
        /// </summary>
        private void LoadDebugMenuSettings()
        {
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

        #endregion
    }
}
