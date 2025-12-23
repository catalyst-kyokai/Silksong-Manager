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

        #region Private Fields

        /// <summary>Current game speed value.</summary>
        private float _gameSpeed = 1f;

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
            DebugMenuStyles.DrawSectionHeader("GAME SPEED");

            GUILayout.Label($"Speed: {_gameSpeed:F1}x", DebugMenuStyles.Label);
            _gameSpeed = GUILayout.HorizontalSlider(_gameSpeed, 0.1f, 5f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0.5x", DebugMenuStyles.ButtonSmall)) _gameSpeed = 0.5f;
            if (GUILayout.Button("1x", DebugMenuStyles.ButtonSmall)) _gameSpeed = 1f;
            if (GUILayout.Button("2x", DebugMenuStyles.ButtonSmall)) _gameSpeed = 2f;
            if (GUILayout.Button("Apply", DebugMenuStyles.ButtonSmall)) World.WorldActions.SetGameSpeed(_gameSpeed);
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
