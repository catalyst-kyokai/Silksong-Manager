using System;
using UnityEngine;
using SilksongManager.Menu.Keybinds;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// In-menu keybind editor window.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class KeybindsWindow : BaseWindow
    {
        public override int WindowId => 10006;
        public override string Title => "Keybinds";
        protected override Vector2 DefaultSize => new Vector2(320, 400);
        
        private ModAction? _listeningAction = null;
        
        protected override void DrawContent()
        {
            DebugMenuStyles.DrawSectionHeader("MOD KEYBINDS");
            
            GUILayout.Label("Click a keybind to change it. Press Escape to cancel.", 
                DebugMenuStyles.Label);
            
            GUILayout.Space(8);
            
            // List all actions
            foreach (ModAction action in Enum.GetValues(typeof(ModAction)))
            {
                DrawKeybindRow(action);
            }
            
            GUILayout.FlexibleSpace();
            
            DebugMenuStyles.DrawSeparator();
            
            if (GUILayout.Button("Reset All to Defaults", DebugMenuStyles.Button))
            {
                ModKeybindManager.ResetToDefaults();
            }
        }
        
        private void DrawKeybindRow(ModAction action)
        {
            GUILayout.BeginHorizontal();
            
            // Action name
            GUILayout.Label(ModKeybindManager.GetActionName(action), DebugMenuStyles.Label, GUILayout.Width(140));
            
            // Keybind button
            var currentKey = ModKeybindManager.GetKeybind(action);
            bool isListening = _listeningAction == action;
            
            if (DebugMenuStyles.DrawKeybindButton(currentKey, isListening))
            {
                if (isListening)
                {
                    // Cancel listening
                    _listeningAction = null;
                }
                else
                {
                    // Start listening
                    _listeningAction = action;
                }
            }
            
            // Clear button
            if (GUILayout.Button("×", DebugMenuStyles.CloseButton))
            {
                ModKeybindManager.SetKeybind(action, KeyCode.None);
            }
            
            GUILayout.EndHorizontal();
        }
        
        public override void Update()
        {
            base.Update();
            
            // Handle key listening
            if (_listeningAction.HasValue)
            {
                // Check for escape to cancel
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _listeningAction = null;
                    return;
                }
                
                // Check for any key press
                foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                {
                    if (key == KeyCode.None || key == KeyCode.Mouse0 || key == KeyCode.Mouse1)
                        continue;
                    
                    if (Input.GetKeyDown(key))
                    {
                        ModKeybindManager.SetKeybind(_listeningAction.Value, key);
                        _listeningAction = null;
                        return;
                    }
                }
            }
        }
    }
}
