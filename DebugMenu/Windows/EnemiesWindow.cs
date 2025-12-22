using UnityEngine;
using SilksongManager.Menu.Keybinds;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Enemies manipulation window.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class EnemiesWindow : BaseWindow
    {
        public override int WindowId => 10004;
        public override string Title => "Enemies";
        protected override Vector2 DefaultSize => new Vector2(280, 300);
        
        protected override void DrawContent()
        {
            // Quick actions
            DebugMenuStyles.DrawSectionHeader("ACTIONS");
            
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Kill All", DebugMenuStyles.Button))
            {
                Enemies.EnemyActions.KillAllEnemies();
            }
            DrawKeybindHint(ModAction.KillAllEnemies);
            
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            
            bool isFrozen = Enemies.EnemyActions.AreEnemiesFrozen;
            if (DebugMenuStyles.DrawToggleButton(isFrozen ? "Freeze ✓" : "Freeze", isFrozen))
            {
                Enemies.EnemyActions.FreezeEnemies(!isFrozen);
            }
            DrawKeybindHint(ModAction.FreezeEnemies);
            
            GUILayout.EndHorizontal();
            
            // Stats
            DebugMenuStyles.DrawSectionHeader("STATS");
            
            int enemyCount = Enemies.EnemyActions.GetEnemyCount();
            GUILayout.Label($"Enemies in scene: {enemyCount}", DebugMenuStyles.Label);
            
            DebugMenuStyles.DrawStatus("Frozen", isFrozen);
        }
        
        private void DrawKeybindHint(ModAction action)
        {
            var key = ModKeybindManager.GetKeybind(action);
            if (key != KeyCode.None)
            {
                GUILayout.Label($"[{DebugMenuStyles.KeyCodeToString(key)}]", DebugMenuStyles.Label, GUILayout.Width(50));
            }
        }
    }
}
