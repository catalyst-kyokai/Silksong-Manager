using UnityEngine;
using SilksongManager.Menu.Keybinds;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// World manipulation window.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class WorldWindow : BaseWindow
    {
        #region Window Properties

        public override int WindowId => 10003;
        public override string Title => "World";
        protected override Vector2 DefaultSize => new Vector2(300, 380);

        #endregion

        #region Drawing Methods

        protected override void DrawContent()
        {
            DebugMenuStyles.DrawSectionHeader("POSITION");

            var hero = Plugin.Hero;
            if (hero != null)
            {
                var pos = hero.transform.position;
                GUILayout.Label($"X: {pos.x:F2}  Y: {pos.y:F2}  Z: {pos.z:F2}", DebugMenuStyles.Label);
            }
            else
            {
                GUILayout.Label("Not in game", DebugMenuStyles.LabelCentered);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Pos", DebugMenuStyles.Button))
            {
                World.WorldActions.SavePosition();
            }
            DrawKeybindHint(ModAction.SavePosition);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Pos", DebugMenuStyles.Button))
            {
                World.WorldActions.LoadPosition();
            }
            DrawKeybindHint(ModAction.LoadPosition);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            DebugMenuStyles.DrawSectionHeader("ACTIONS");

            if (GUILayout.Button("Reload Scene", DebugMenuStyles.Button))
            {
                World.WorldActions.ReloadCurrentScene();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause", DebugMenuStyles.Button))
            {
                World.WorldActions.PauseGame();
            }
            if (GUILayout.Button("Resume", DebugMenuStyles.Button))
            {
                World.WorldActions.ResumeGame();
            }
            GUILayout.EndHorizontal();

            DebugMenuStyles.DrawSectionHeader("SCENE");

            var gm = Plugin.GM;
            if (gm != null)
            {
                GUILayout.Label($"Current: {gm.sceneName}", DebugMenuStyles.Label);
            }
        }

        #endregion

        #region Helpers

        private void DrawKeybindHint(ModAction action)
        {
            var key = ModKeybindManager.GetKeybind(action);
            if (key != KeyCode.None)
            {
                GUILayout.Label($"[{DebugMenuStyles.KeyCodeToString(key)}]", DebugMenuStyles.Label, GUILayout.Width(50));
            }
        }

        #endregion
    }
}
