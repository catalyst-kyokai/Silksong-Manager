using UnityEngine;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Speed control debug window.
    /// Provides sliders and buttons for controlling game, player, enemy, and environment speeds.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class SpeedControlWindow : BaseWindow
    {
        #region Window Properties

        public override int WindowId => 10010;
        public override string Title => "Speed Control";
        protected override Vector2 DefaultSize => new Vector2(320, 520);

        #endregion

        #region Drawing Methods

        protected override void DrawContent()
        {
            // Global Speed Section
            DebugMenuStyles.DrawSectionHeader("GLOBAL SPEED");
            float globalSpeed = SpeedControl.SpeedControlConfig.GlobalSpeed;
            float newGlobalSpeed = DrawSpeedSlider("Time Scale", globalSpeed, 0.1f, 5f);
            if (newGlobalSpeed != globalSpeed)
            {
                SpeedControl.SpeedControlManager.SetGlobalSpeed(newGlobalSpeed);
            }
            DrawPresetButtonsGlobal();

            GUILayout.Space(10);

            // Player Speed Section
            DebugMenuStyles.DrawSectionHeader("PLAYER SPEED");

            float playerMove = SpeedControl.SpeedControlConfig.PlayerMovementSpeed;
            float newPlayerMove = DrawSpeedSlider("Movement", playerMove, 0.1f, 5f);
            if (newPlayerMove != playerMove)
            {
                SpeedControl.SpeedControlManager.SetPlayerMovementSpeed(newPlayerMove);
            }

            float playerAtk = SpeedControl.SpeedControlConfig.PlayerAttackSpeed;
            float newPlayerAtk = DrawSpeedSlider("Attack", playerAtk, 0.1f, 5f);
            if (newPlayerAtk != playerAtk)
            {
                SpeedControl.SpeedControlManager.SetPlayerAttackSpeed(newPlayerAtk);
            }

            float playerAll = SpeedControl.SpeedControlConfig.PlayerAllSpeed;
            float newPlayerAll = DrawSpeedSlider("All (Combined)", playerAll, 0.1f, 5f);
            if (newPlayerAll != playerAll)
            {
                SpeedControl.SpeedControlManager.SetPlayerAllSpeed(newPlayerAll);
            }

            GUILayout.Space(10);

            // Enemy Speed Section
            DebugMenuStyles.DrawSectionHeader("ENEMY SPEED");

            float enemyMove = SpeedControl.SpeedControlConfig.EnemyMovementSpeed;
            float newEnemyMove = DrawSpeedSlider("Movement", enemyMove, 0.1f, 5f);
            if (newEnemyMove != enemyMove)
            {
                SpeedControl.SpeedControlManager.SetEnemyMovementSpeed(newEnemyMove);
            }

            float enemyAtk = SpeedControl.SpeedControlConfig.EnemyAttackSpeed;
            float newEnemyAtk = DrawSpeedSlider("Attack", enemyAtk, 0.1f, 5f);
            if (newEnemyAtk != enemyAtk)
            {
                SpeedControl.SpeedControlManager.SetEnemyAttackSpeed(newEnemyAtk);
            }

            float enemyAll = SpeedControl.SpeedControlConfig.EnemyAllSpeed;
            float newEnemyAll = DrawSpeedSlider("All (Combined)", enemyAll, 0.1f, 5f);
            if (newEnemyAll != enemyAll)
            {
                SpeedControl.SpeedControlManager.SetEnemyAllSpeed(newEnemyAll);
            }

            GUILayout.Space(10);

            // Environment Speed Section
            DebugMenuStyles.DrawSectionHeader("ENVIRONMENT SPEED");
            float envSpeed = SpeedControl.SpeedControlConfig.EnvironmentSpeed;
            float newEnvSpeed = DrawSpeedSlider("Environment", envSpeed, 0.1f, 5f);
            if (newEnvSpeed != envSpeed)
            {
                SpeedControl.SpeedControlManager.SetEnvironmentSpeed(newEnvSpeed);
            }

            GUILayout.Space(15);

            // Reset All Button
            DebugMenuStyles.DrawSeparator();
            if (GUILayout.Button("RESET ALL TO 1.0x", DebugMenuStyles.Button))
            {
                SpeedControl.SpeedControlManager.ResetAll();
                UI.NotificationManager.Show("Speed Reset", "All speeds set to 1.0x");
            }

            // Status Display
            GUILayout.Space(10);
            DrawStatusDisplay();
        }

        #endregion

        #region Helper Methods

        private float DrawSpeedSlider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:F2}x", DebugMenuStyles.Label, GUILayout.Width(140));
            float newValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            return newValue;
        }

        private void DrawPresetButtonsGlobal()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0.25x", DebugMenuStyles.ButtonSmall))
            {
                SpeedControl.SpeedControlManager.SetGlobalSpeed(0.25f);
            }
            if (GUILayout.Button("0.5x", DebugMenuStyles.ButtonSmall))
            {
                SpeedControl.SpeedControlManager.SetGlobalSpeed(0.5f);
            }
            if (GUILayout.Button("1x", DebugMenuStyles.ButtonSmall))
            {
                SpeedControl.SpeedControlManager.SetGlobalSpeed(1f);
            }
            if (GUILayout.Button("2x", DebugMenuStyles.ButtonSmall))
            {
                SpeedControl.SpeedControlManager.SetGlobalSpeed(2f);
            }
            if (GUILayout.Button("5x", DebugMenuStyles.ButtonSmall))
            {
                SpeedControl.SpeedControlManager.SetGlobalSpeed(5f);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawStatusDisplay()
        {
            GUILayout.Label("Current Status:", DebugMenuStyles.LabelBold);

            // Show effective values
            string status = $"Time Scale: {Time.timeScale:F2}x";

            GUILayout.Label(status, DebugMenuStyles.Label);
        }

        #endregion
    }
}
