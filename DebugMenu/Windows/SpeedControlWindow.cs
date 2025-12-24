using UnityEngine;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Speed control debug window.
    /// Provides manual input and buttons for controlling game, player, enemy speeds.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class SpeedControlWindow : BaseWindow
    {
        #region Window Properties

        public override int WindowId => 10012;
        public override string Title => "Speed Control";
        protected override Vector2 DefaultSize => new Vector2(340, 520);

        #endregion

        #region Private Fields

        private string _globalInput = "1.00";
        private string _playerMoveInput = "1.00";
        private string _playerAtkInput = "1.00";
        private string _playerAllInput = "1.00";
        private string _enemyMoveInput = "1.00";
        private string _enemyAtkInput = "1.00";
        private string _enemyAllInput = "1.00";

        #endregion

        #region Drawing Methods

        protected override void DrawContent()
        {
            // Global Speed Section
            DebugMenuStyles.DrawSectionHeader("GLOBAL SPEED");
            DrawSpeedInput("Time Scale", ref _globalInput,
                () => SpeedControl.SpeedControlConfig.GlobalSpeed,
                v => SpeedControl.SpeedControlManager.SetGlobalSpeed(v));
            DrawPresetButtons(new[] { 0.25f, 0.5f, 1f, 2f, 5f },
                v => SpeedControl.SpeedControlManager.SetGlobalSpeed(v),
                ref _globalInput);

            GUILayout.Space(10);

            // Player Speed Section
            DebugMenuStyles.DrawSectionHeader("PLAYER SPEED");
            DrawSpeedInput("Movement", ref _playerMoveInput,
                () => SpeedControl.SpeedControlConfig.PlayerMovementSpeed,
                v => SpeedControl.SpeedControlManager.SetPlayerMovementSpeed(v));
            DrawSpeedInput("Attack", ref _playerAtkInput,
                () => SpeedControl.SpeedControlConfig.PlayerAttackSpeed,
                v => SpeedControl.SpeedControlManager.SetPlayerAttackSpeed(v));
            DrawSpeedInput("All (Combined)", ref _playerAllInput,
                () => SpeedControl.SpeedControlConfig.PlayerAllSpeed,
                v => SpeedControl.SpeedControlManager.SetPlayerAllSpeed(v));

            GUILayout.Space(10);

            // Enemy Speed Section
            DebugMenuStyles.DrawSectionHeader("ENEMY SPEED");
            DrawSpeedInput("Movement", ref _enemyMoveInput,
                () => SpeedControl.SpeedControlConfig.EnemyMovementSpeed,
                v => SpeedControl.SpeedControlManager.SetEnemyMovementSpeed(v));
            DrawSpeedInput("Attack", ref _enemyAtkInput,
                () => SpeedControl.SpeedControlConfig.EnemyAttackSpeed,
                v => SpeedControl.SpeedControlManager.SetEnemyAttackSpeed(v));
            DrawSpeedInput("All (Combined)", ref _enemyAllInput,
                () => SpeedControl.SpeedControlConfig.EnemyAllSpeed,
                v => SpeedControl.SpeedControlManager.SetEnemyAllSpeed(v));

            GUILayout.Space(15);

            // Reset All Button
            DebugMenuStyles.DrawSeparator();
            if (GUILayout.Button("RESET ALL TO 1.0x", DebugMenuStyles.Button))
            {
                SpeedControl.SpeedControlManager.ResetAll();
                RefreshInputs();
                UI.NotificationManager.Show("Speed Reset", "All speeds reset to 1.0x");
            }

            // Status Display
            GUILayout.Space(10);
            DrawStatusDisplay();
        }

        #endregion

        #region Helper Methods

        private void DrawSpeedInput(string label, ref string inputField,
            System.Func<float> getValue, System.Action<float> setValue,
            float step = 0.1f, float largeStep = 1f)
        {
            float currentValue = getValue();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}:", DebugMenuStyles.Label, GUILayout.Width(100));

            // Minus buttons
            if (GUILayout.Button($"-{largeStep}", DebugMenuStyles.ButtonSmall, GUILayout.Width(35)))
            {
                float newVal = Mathf.Max(0.1f, currentValue - largeStep);
                setValue(newVal);
                inputField = newVal.ToString("F2");
            }
            if (GUILayout.Button($"-{step}", DebugMenuStyles.ButtonSmall, GUILayout.Width(35)))
            {
                float newVal = Mathf.Max(0.1f, currentValue - step);
                setValue(newVal);
                inputField = newVal.ToString("F2");
            }

            // Text input
            string newInput = GUILayout.TextField(inputField, DebugMenuStyles.TextField, GUILayout.Width(55));
            if (newInput != inputField)
            {
                inputField = newInput;
                if (float.TryParse(newInput, out float val) && val > 0)
                {
                    setValue(val);
                }
            }

            // Plus buttons
            if (GUILayout.Button($"+{step}", DebugMenuStyles.ButtonSmall, GUILayout.Width(35)))
            {
                float newVal = currentValue + step;
                setValue(newVal);
                inputField = newVal.ToString("F2");
            }
            if (GUILayout.Button($"+{largeStep}", DebugMenuStyles.ButtonSmall, GUILayout.Width(35)))
            {
                float newVal = currentValue + largeStep;
                setValue(newVal);
                inputField = newVal.ToString("F2");
            }

            GUILayout.EndHorizontal();
        }

        private void DrawPresetButtons(float[] presets, System.Action<float> setValue, ref string inputField)
        {
            GUILayout.BeginHorizontal();
            foreach (float preset in presets)
            {
                if (GUILayout.Button($"{preset}x", DebugMenuStyles.ButtonSmall))
                {
                    setValue(preset);
                    inputField = preset.ToString("F2");
                }
            }
            GUILayout.EndHorizontal();
        }

        private void RefreshInputs()
        {
            _globalInput = SpeedControl.SpeedControlConfig.GlobalSpeed.ToString("F2");
            _playerMoveInput = SpeedControl.SpeedControlConfig.PlayerMovementSpeed.ToString("F2");
            _playerAtkInput = SpeedControl.SpeedControlConfig.PlayerAttackSpeed.ToString("F2");
            _playerAllInput = SpeedControl.SpeedControlConfig.PlayerAllSpeed.ToString("F2");
            _enemyMoveInput = SpeedControl.SpeedControlConfig.EnemyMovementSpeed.ToString("F2");
            _enemyAtkInput = SpeedControl.SpeedControlConfig.EnemyAttackSpeed.ToString("F2");
            _enemyAllInput = SpeedControl.SpeedControlConfig.EnemyAllSpeed.ToString("F2");
        }

        private void DrawStatusDisplay()
        {
            GUILayout.Label("Current Status:", DebugMenuStyles.LabelBold);
            GUILayout.Label($"Time Scale: {Time.timeScale:F2}x", DebugMenuStyles.Label);
        }

        #endregion
    }
}
