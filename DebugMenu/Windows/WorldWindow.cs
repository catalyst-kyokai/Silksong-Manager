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
        public override int WindowId => 10003;
        public override string Title => "World";
        protected override Vector2 DefaultSize => new Vector2(300, 380);

        private float _gameSpeed = 1f;

        protected override void DrawContent()
        {
            // Position section
            DebugMenuStyles.DrawSectionHeader("POSITION");

            var hero = Plugin.Hero;
            if (hero != null)
            {
                var pos = hero.transform.position;
                GUILayout.Label($"X: {pos.x:F2}  Y: {pos.y:F2}  Z: {pos.z:F2}", DebugMenuStyles.Label);
            }
            else
            {
                GUILayout.Label("Position: N/A", DebugMenuStyles.Label);
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Save", DebugMenuStyles.Button))
            {
                World.WorldActions.SavePosition();
            }
            DrawKeybindHint(ModAction.SavePosition);

            if (GUILayout.Button("Load", DebugMenuStyles.Button))
            {
                World.WorldActions.LoadPosition();
            }
            DrawKeybindHint(ModAction.LoadPosition);

            GUILayout.EndHorizontal();

            // Game speed section
            DebugMenuStyles.DrawSectionHeader("GAME SPEED");

            _gameSpeed = Time.timeScale;

            GUILayout.Label($"Current: {_gameSpeed:F2}x", DebugMenuStyles.Label);

            GUILayout.BeginHorizontal();
            GUILayout.Label("0.1", DebugMenuStyles.Label, GUILayout.Width(25));
            _gameSpeed = GUILayout.HorizontalSlider(_gameSpeed, 0.1f, 5f);
            GUILayout.Label("5.0", DebugMenuStyles.Label, GUILayout.Width(25));
            GUILayout.EndHorizontal();

            if (Mathf.Abs(_gameSpeed - Time.timeScale) > 0.01f)
            {
                World.WorldActions.SetGameSpeed(_gameSpeed);
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("0.5x", DebugMenuStyles.ButtonSmall))
            {
                World.WorldActions.SetGameSpeed(0.5f);
            }
            if (GUILayout.Button("1x", DebugMenuStyles.ButtonSmall))
            {
                World.WorldActions.SetGameSpeed(1f);
            }
            if (GUILayout.Button("2x", DebugMenuStyles.ButtonSmall))
            {
                World.WorldActions.SetGameSpeed(2f);
            }
            if (GUILayout.Button("5x", DebugMenuStyles.ButtonSmall))
            {
                World.WorldActions.SetGameSpeed(5f);
            }

            GUILayout.EndHorizontal();

            // Scene info
            DebugMenuStyles.DrawSectionHeader("SCENE");

            var gm = Plugin.GM;
            if (gm != null)
            {
                GUILayout.Label($"Scene: {gm.sceneName}", DebugMenuStyles.Label);
            }
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
