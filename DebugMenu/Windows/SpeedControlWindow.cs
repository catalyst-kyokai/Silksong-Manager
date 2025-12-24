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
        protected override Vector2 DefaultSize => new Vector2(320, 500);

        #endregion

        #region Private Fields

        // Local copies for slider display
        private float _globalSpeed = 1f;
        private float _playerMovement = 1f;
        private float _playerAttack = 1f;
        private float _playerAll = 1f;
        private float _enemyMovement = 1f;
        private float _enemyAttack = 1f;
        private float _enemyAll = 1f;
        private float _environmentSpeed = 1f;

        #endregion

        #region Drawing Methods

        protected override void DrawContent()
        {
            // Sync local values from config
            SyncFromConfig();

            // Global Speed Section
            DebugMenuStyles.DrawSectionHeader("GLOBAL SPEED");
            DrawSpeedSlider("Time Scale", ref _globalSpeed, 0.1f, 5f);
            DrawPresetButtons(ref _globalSpeed, new float[] { 0.25f, 0.5f, 1f, 2f, 5f });
            if (GUILayout.Button("Apply Global", DebugMenuStyles.Button))
            {
                SpeedControl.SpeedControlManager.SetGlobalSpeed(_globalSpeed);
            }

            GUILayout.Space(10);

            // Player Speed Section
            DebugMenuStyles.DrawSectionHeader("PLAYER SPEED");

            DrawSpeedSlider("Movement", ref _playerMovement, 0.1f, 5f);
            if (GUILayout.Button("Apply Movement", DebugMenuStyles.ButtonSmall))
            {
                SpeedControl.SpeedControlManager.SetPlayerMovementSpeed(_playerMovement);
            }

            DrawSpeedSlider("Attack", ref _playerAttack, 0.1f, 5f);
            if (GUILayout.Button("Apply Attack", DebugMenuStyles.ButtonSmall))
            {
                SpeedControl.SpeedControlManager.SetPlayerAttackSpeed(_playerAttack);
            }

            DrawSpeedSlider("All (Combined)", ref _playerAll, 0.1f, 5f);
            if (GUILayout.Button("Apply All Player", DebugMenuStyles.ButtonSmall))
            {
                SpeedControl.SpeedControlManager.SetPlayerAllSpeed(_playerAll);
            }

            GUILayout.Space(10);

            // Enemy Speed Section
            DebugMenuStyles.DrawSectionHeader("ENEMY SPEED");

            DrawSpeedSlider("Movement", ref _enemyMovement, 0.1f, 5f);
            if (GUILayout.Button("Apply Movement", DebugMenuStyles.ButtonSmall))
            {
                SpeedControl.SpeedControlManager.SetEnemyMovementSpeed(_enemyMovement);
            }

            DrawSpeedSlider("Attack", ref _enemyAttack, 0.1f, 5f);
            if (GUILayout.Button("Apply Attack", DebugMenuStyles.ButtonSmall))
            {
                SpeedControl.SpeedControlManager.SetEnemyAttackSpeed(_enemyAttack);
            }

            DrawSpeedSlider("All (Combined)", ref _enemyAll, 0.1f, 5f);
            if (GUILayout.Button("Apply All Enemy", DebugMenuStyles.ButtonSmall))
            {
                SpeedControl.SpeedControlManager.SetEnemyAllSpeed(_enemyAll);
            }

            GUILayout.Space(10);

            // Environment Speed Section
            DebugMenuStyles.DrawSectionHeader("ENVIRONMENT SPEED");
            DrawSpeedSlider("Environment", ref _environmentSpeed, 0.1f, 5f);
            if (GUILayout.Button("Apply Environment", DebugMenuStyles.Button))
            {
                SpeedControl.SpeedControlManager.SetEnvironmentSpeed(_environmentSpeed);
            }

            GUILayout.Space(15);

            // Reset All Button
            DebugMenuStyles.DrawSeparator();
            if (GUILayout.Button("RESET ALL TO 1.0x", DebugMenuStyles.Button))
            {
                SpeedControl.SpeedControlManager.ResetAll();
                ResetLocalValues();
                UI.NotificationManager.Show("Speed Reset", "All speeds set to 1.0x");
            }

            // Status Display
            GUILayout.Space(10);
            DrawStatusDisplay();
        }

        #endregion

        #region Helper Methods

        private void SyncFromConfig()
        {
            _globalSpeed = SpeedControl.SpeedControlConfig.GlobalSpeed;
            _playerMovement = SpeedControl.SpeedControlConfig.PlayerMovementSpeed;
            _playerAttack = SpeedControl.SpeedControlConfig.PlayerAttackSpeed;
            _playerAll = SpeedControl.SpeedControlConfig.PlayerAllSpeed;
            _enemyMovement = SpeedControl.SpeedControlConfig.EnemyMovementSpeed;
            _enemyAttack = SpeedControl.SpeedControlConfig.EnemyAttackSpeed;
            _enemyAll = SpeedControl.SpeedControlConfig.EnemyAllSpeed;
            _environmentSpeed = SpeedControl.SpeedControlConfig.EnvironmentSpeed;
        }

        private void ResetLocalValues()
        {
            _globalSpeed = 1f;
            _playerMovement = 1f;
            _playerAttack = 1f;
            _playerAll = 1f;
            _enemyMovement = 1f;
            _enemyAttack = 1f;
            _enemyAll = 1f;
            _environmentSpeed = 1f;
        }

        private void DrawSpeedSlider(string label, ref float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:F2}x", DebugMenuStyles.Label, GUILayout.Width(140));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        private void DrawPresetButtons(ref float value, float[] presets)
        {
            GUILayout.BeginHorizontal();
            foreach (var preset in presets)
            {
                if (GUILayout.Button($"{preset}x", DebugMenuStyles.ButtonSmall))
                {
                    value = preset;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawStatusDisplay()
        {
            GUILayout.Label("Current Status:", DebugMenuStyles.Label);

            // Show effective values
            string status = $"Global: {SpeedControl.SpeedControlConfig.GlobalSpeed:F2}x";
            if (SpeedControl.SpeedControlConfig.EffectivePlayerMovement != 1f || SpeedControl.SpeedControlConfig.EffectivePlayerAttack != 1f)
            {
                status += $"\nPlayer Move: {SpeedControl.SpeedControlConfig.EffectivePlayerMovement:F2}x, Atk: {SpeedControl.SpeedControlConfig.EffectivePlayerAttack:F2}x";
            }
            if (SpeedControl.SpeedControlConfig.EffectiveEnemyMovement != 1f || SpeedControl.SpeedControlConfig.EffectiveEnemyAttack != 1f)
            {
                status += $"\nEnemy Move: {SpeedControl.SpeedControlConfig.EffectiveEnemyMovement:F2}x, Atk: {SpeedControl.SpeedControlConfig.EffectiveEnemyAttack:F2}x";
            }
            if (SpeedControl.SpeedControlConfig.EnvironmentSpeed != 1f)
            {
                status += $"\nEnvironment: {SpeedControl.SpeedControlConfig.EnvironmentSpeed:F2}x";
            }

            GUILayout.Label(status, DebugMenuStyles.Label);
        }

        #endregion
    }
}
