using UnityEngine;
using SilksongManager.Damage;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Combat settings window for damage customization.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class CombatWindow : BaseWindow
    {
        public override int WindowId => 10009;
        public override string Title => "Combat";
        protected override Vector2 DefaultSize => new Vector2(320, 500);

        // Input fields
        private string _nailInput = "5";
        private string _toolInput = "10";
        private string _spellInput = "15";
        private string _summonInput = "8";
        private string _globalMultInput = "1";

        protected override void DrawContent()
        {
            // Nail Damage
            DrawDamageSection("NAIL DAMAGE", DamageType.Nail, ref _nailInput);

            GUILayout.Space(8);

            // Tool Damage
            DrawDamageSection("TOOL DAMAGE", DamageType.Tool, ref _toolInput);

            GUILayout.Space(8);

            // Spell Damage
            DrawDamageSection("SPELL DAMAGE", DamageType.Spell, ref _spellInput);

            GUILayout.Space(8);

            // Summon Damage
            DrawDamageSection("SUMMON DAMAGE", DamageType.Summon, ref _summonInput);

            GUILayout.Space(12);

            // Global Multiplier
            DrawGlobalMultiplier();
        }

        private void DrawDamageSection(string header, DamageType type, ref string inputField)
        {
            DebugMenuStyles.DrawSectionHeader(header);

            bool enabled = DamageSystem.IsCustomEnabled(type);
            float currentDamage = DamageSystem.GetDamage(type);
            float multiplier = DamageSystem.GetMultiplier(type);

            // Enable toggle
            GUILayout.BeginHorizontal();
            if (DebugMenuStyles.DrawToggleButton(enabled ? "Custom ON" : "Custom OFF", enabled))
            {
                DamageSystem.ToggleCustomDamage(type);
            }
            GUILayout.EndHorizontal();

            if (enabled)
            {
                // Damage value with +/- buttons
                GUILayout.BeginHorizontal();
                GUILayout.Label("Value:", DebugMenuStyles.Label, GUILayout.Width(50));

                if (GUILayout.Button("-10", DebugMenuStyles.Button, GUILayout.Width(40)))
                {
                    DamageSystem.AdjustDamage(type, -10f);
                    inputField = DamageSystem.GetDamage(type).ToString("F1");
                }
                if (GUILayout.Button("-1", DebugMenuStyles.Button, GUILayout.Width(30)))
                {
                    DamageSystem.AdjustDamage(type, -1f);
                    inputField = DamageSystem.GetDamage(type).ToString("F1");
                }

                // Text field for direct input
                string newInput = GUILayout.TextField(inputField, DebugMenuStyles.TextField, GUILayout.Width(60));
                if (newInput != inputField)
                {
                    inputField = newInput;
                    if (float.TryParse(newInput, out float val))
                    {
                        DamageSystem.SetDamage(type, val);
                    }
                }

                if (GUILayout.Button("+1", DebugMenuStyles.Button, GUILayout.Width(30)))
                {
                    DamageSystem.AdjustDamage(type, 1f);
                    inputField = DamageSystem.GetDamage(type).ToString("F1");
                }
                if (GUILayout.Button("+10", DebugMenuStyles.Button, GUILayout.Width(40)))
                {
                    DamageSystem.AdjustDamage(type, 10f);
                    inputField = DamageSystem.GetDamage(type).ToString("F1");
                }
                GUILayout.EndHorizontal();

                // Multiplier
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Mult: {multiplier:F2}x", DebugMenuStyles.Label, GUILayout.Width(80));
                float newMult = GUILayout.HorizontalSlider(multiplier, -2f, 10f);
                if (Mathf.Abs(newMult - multiplier) > 0.01f)
                {
                    DamageSystem.SetMultiplier(type, newMult);
                }
                GUILayout.EndHorizontal();

                // Show final damage
                float finalDamage = DamageSystem.CalculateFinalDamage(type, currentDamage);
                Color color = finalDamage >= 0 ? DebugMenuStyles.StatusOn : DebugMenuStyles.StatusWarning;
                var oldColor = GUI.color;
                GUI.color = color;
                GUILayout.Label($"Final: {finalDamage:F1}", DebugMenuStyles.LabelBold);
                GUI.color = oldColor;
            }
        }

        private void DrawGlobalMultiplier()
        {
            DebugMenuStyles.DrawSectionHeader("GLOBAL MULTIPLIER");

            float globalMult = DamageSystem.GlobalMultiplier;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Global: {globalMult:F2}x", DebugMenuStyles.Label, GUILayout.Width(100));

            if (GUILayout.Button("0.5x", DebugMenuStyles.Button))
                DamageSystem.GlobalMultiplier = 0.5f;
            if (GUILayout.Button("1x", DebugMenuStyles.Button))
                DamageSystem.GlobalMultiplier = 1f;
            if (GUILayout.Button("2x", DebugMenuStyles.Button))
                DamageSystem.GlobalMultiplier = 2f;
            if (GUILayout.Button("10x", DebugMenuStyles.Button))
                DamageSystem.GlobalMultiplier = 10f;

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            float newGlobal = GUILayout.HorizontalSlider(globalMult, -5f, 100f);
            if (Mathf.Abs(newGlobal - globalMult) > 0.01f)
            {
                DamageSystem.GlobalMultiplier = newGlobal;
            }
            GUILayout.EndHorizontal();

            // Input field for exact value
            GUILayout.BeginHorizontal();
            GUILayout.Label("Exact:", DebugMenuStyles.Label, GUILayout.Width(50));
            string newInput = GUILayout.TextField(_globalMultInput, DebugMenuStyles.TextField, GUILayout.Width(60));
            if (newInput != _globalMultInput)
            {
                _globalMultInput = newInput;
                if (float.TryParse(newInput, out float val))
                {
                    DamageSystem.GlobalMultiplier = val;
                }
            }
            if (GUILayout.Button("Apply", DebugMenuStyles.Button))
            {
                if (float.TryParse(_globalMultInput, out float val))
                {
                    DamageSystem.GlobalMultiplier = val;
                }
            }
            GUILayout.EndHorizontal();

            // Info
            GUILayout.Space(4);
            GUILayout.Label("Negative = Heal enemies", DebugMenuStyles.Label);
            GUILayout.Label("Zero = No damage", DebugMenuStyles.Label);
        }
    }
}
