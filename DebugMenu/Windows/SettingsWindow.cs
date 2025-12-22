using UnityEngine;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Debug menu settings window.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class SettingsWindow : BaseWindow
    {
        public override int WindowId => 10007;
        public override string Title => "Settings";
        protected override Vector2 DefaultSize => new Vector2(280, 300);
        
        protected override void DrawContent()
        {
            // Opacity settings
            DebugMenuStyles.DrawSectionHeader("TRANSPARENCY");
            
            // Opacity mode toggle
            bool isFullMode = DebugMenuConfig.CurrentOpacityMode == DebugMenuConfig.OpacityMode.FullMenu;
            
            GUILayout.BeginHorizontal();
            if (DebugMenuStyles.DrawToggleButton("Background Only", !isFullMode))
            {
                DebugMenuConfig.CurrentOpacityMode = DebugMenuConfig.OpacityMode.BackgroundOnly;
            }
            if (DebugMenuStyles.DrawToggleButton("Full Menu", isFullMode))
            {
                DebugMenuConfig.CurrentOpacityMode = DebugMenuConfig.OpacityMode.FullMenu;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(8);
            
            // Background opacity slider
            GUILayout.Label($"Background Opacity: {DebugMenuConfig.BackgroundOpacity:P0}", DebugMenuStyles.Label);
            DebugMenuConfig.BackgroundOpacity = GUILayout.HorizontalSlider(
                DebugMenuConfig.BackgroundOpacity, 0.1f, 1f);
            
            GUILayout.Space(4);
            
            // Full menu opacity slider (only visible in Full mode)
            if (isFullMode)
            {
                GUILayout.Label($"Menu Opacity: {DebugMenuConfig.FullMenuOpacity:P0}", DebugMenuStyles.Label);
                DebugMenuConfig.FullMenuOpacity = GUILayout.HorizontalSlider(
                    DebugMenuConfig.FullMenuOpacity, 0.3f, 1f);
            }
            
            // Window management
            DebugMenuStyles.DrawSectionHeader("WINDOWS");
            
            if (GUILayout.Button("Reset Window Positions", DebugMenuStyles.Button))
            {
                // Reset positions (would need to implement this in each window)
                Plugin.Log.LogInfo("Window positions reset");
            }
            
            // Info
            DebugMenuStyles.DrawSectionHeader("INFO");
            
            GUILayout.Label($"Version: {PluginInfo.VERSION}", DebugMenuStyles.Label);
            GUILayout.Label("Author: Catalyst", DebugMenuStyles.Label);
            GUILayout.Label("Telegram: @Catalyst_Kyokai", DebugMenuStyles.Label);
        }
    }
}
