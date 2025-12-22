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
            // Keybinds button
            AddButton("Keybinds", () =>
            {
                var keybindsScreen = new KeybindsMenuScreen();
                MenuNavigation.Show(keybindsScreen);
            });
            
            // Settings button - future expansion
            // AddButton("Settings", () => { });
            
            // Debug Menu info
            AddButton("Debug Menu", () =>
            {
                // Just show info - debug menu is toggled via hotkey
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
