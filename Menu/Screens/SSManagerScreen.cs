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
            // Settings button - opens SettingsScreen with sliders/toggles
            AddButton("Settings", () =>
            {
                MenuNavigation.Show(new SettingsScreen());
            });

            // Keybinds button
            AddButton("Keybinds", () =>
            {
                MenuNavigation.Show(new KeybindsMenuScreen());
            });

            // Debug Menu info button
            AddButton("Debug Menu", () =>
            {
                var key = ModKeybindManager.GetKeybind(ModAction.ToggleDebugMenu);
                Plugin.Log.LogInfo($"Debug Menu is toggled via {key} key");
            });
        }

        protected override void OnScreenShow(NavigationType navType)
        {
            Plugin.Log.LogInfo("SS Manager screen shown");
        }
    }
}
