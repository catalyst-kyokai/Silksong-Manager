using System;
using SilksongManager.Menu.Core;
using SilksongManager.Menu.Keybinds;

namespace SilksongManager.Menu.Screens
{
    /// <summary>
    /// Main SS Manager menu screen with buttons for mod features.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class SSManagerScreen : CustomMenuScreen
    {
        public SSManagerScreen() : base("SS Manager")
        {
        }

        protected override void BuildContent()
        {
            Plugin.Log.LogInfo($"SSManagerScreen.BuildContent: ContentPane is {(ContentPane != null ? "valid" : "NULL")}");

            // Settings button - opens SettingsScreen with sliders/toggles
            var settingsBtn = AddButton("Settings", () =>
            {
                MenuNavigation.Show(new SettingsScreen());
            });
            Plugin.Log.LogInfo($"SSManagerScreen: Settings button {(settingsBtn != null ? "created" : "FAILED")}");

            // Keybinds button
            var keybindsBtn = AddButton("Keybinds", () =>
            {
                MenuNavigation.Show(new KeybindsMenuScreen());
            });
            Plugin.Log.LogInfo($"SSManagerScreen: Keybinds button {(keybindsBtn != null ? "created" : "FAILED")}");

            // Debug Menu info button
            var debugBtn = AddButton("Debug Menu", () =>
            {
                var key = ModKeybindManager.GetKeybind(ModAction.ToggleDebugMenu);
                Plugin.Log.LogInfo($"Debug Menu is toggled via {key} key");
            });
            Plugin.Log.LogInfo($"SSManagerScreen: Debug Menu button {(debugBtn != null ? "created" : "FAILED")}");
        }

        protected override void OnScreenShow(NavigationType navType)
        {
            Plugin.Log.LogInfo("SS Manager screen shown");
        }
    }
}
