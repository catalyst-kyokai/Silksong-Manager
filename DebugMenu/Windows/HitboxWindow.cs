using UnityEngine;
using SilksongManager.Hitbox;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Hitbox visualization control window.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class HitboxWindow : BaseWindow
    {
        #region Window Properties

        public override int WindowId => 10010;
        public override string Title => "Hitbox Visualizer";
        protected override Vector2 DefaultSize => new Vector2(300, 480);

        #endregion

        #region Drawing Methods

        protected override void DrawContent()
        {
            DebugMenuStyles.DrawSectionHeader("GENERAL");

            bool isEnabled = HitboxConfig.ShowHitboxes;
            if (DebugMenuStyles.DrawToggleButton(isEnabled ? "Enabled ✓" : "Enabled", isEnabled))
            {
                HitboxManager.ToggleHitboxes();
            }

            var key = Menu.Keybinds.ModKeybindManager.GetKeybind(Menu.Keybinds.ModAction.ToggleHitboxes);
            GUILayout.Label($"Keybind: {DebugMenuStyles.KeyCodeToString(key)}", DebugMenuStyles.Label);

            GUILayout.Space(10);
            DebugMenuStyles.DrawSectionHeader("LAYERS");

            DrawLayerToggle("Player", HitboxLayer.Player);
            DrawLayerToggle("Enemy", HitboxLayer.Enemy);
            DrawLayerToggle("Attack", HitboxLayer.Attack);
            DrawLayerToggle("Terrain", HitboxLayer.Terrain);
            DrawLayerToggle("Trigger", HitboxLayer.Trigger);
            DrawLayerToggle("Hazard", HitboxLayer.Hazard);
            DrawLayerToggle("Breakable", HitboxLayer.Breakable);
            DrawLayerToggle("Interactive", HitboxLayer.Interactive);

            GUILayout.Space(10);
            DebugMenuStyles.DrawSectionHeader("SETTINGS");

            GUILayout.Label($"Line Thickness: {HitboxConfig.LineThickness:F1}", DebugMenuStyles.Label);
            HitboxConfig.LineThickness = GUILayout.HorizontalSlider(HitboxConfig.LineThickness, 0.5f, 5f);
        }

        private void DrawLayerToggle(string label, HitboxLayer layer)
        {
            bool current = GetLayerState(layer);
            Color color = GetLayerColor(layer);

            GUILayout.BeginHorizontal();

            var originalColor = GUI.color;
            GUI.color = color;

            // Draw a solid colored square using whiteTexture
            Rect colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(colorRect, Texture2D.whiteTexture);

            GUI.color = originalColor;

            GUILayout.Space(5);

            if (DebugMenuStyles.DrawToggleButton(current ? $"{label} ✓" : label, current))
            {
                SetLayerState(layer, !current);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(2); // Small spacing between rows
        }

        private bool GetLayerState(HitboxLayer layer)
        {
            switch (layer)
            {
                case HitboxLayer.Player: return HitboxConfig.ShowPlayer;
                case HitboxLayer.Enemy: return HitboxConfig.ShowEnemy;
                case HitboxLayer.Attack: return HitboxConfig.ShowAttack;
                case HitboxLayer.Terrain: return HitboxConfig.ShowTerrain;
                case HitboxLayer.Trigger: return HitboxConfig.ShowTrigger;
                case HitboxLayer.Hazard: return HitboxConfig.ShowHazard;
                case HitboxLayer.Breakable: return HitboxConfig.ShowBreakable;
                case HitboxLayer.Interactive: return HitboxConfig.ShowInteractive;
                default: return false;
            }
        }

        private void SetLayerState(HitboxLayer layer, bool state)
        {
            switch (layer)
            {
                case HitboxLayer.Player: HitboxConfig.ShowPlayer = state; break;
                case HitboxLayer.Enemy: HitboxConfig.ShowEnemy = state; break;
                case HitboxLayer.Attack: HitboxConfig.ShowAttack = state; break;
                case HitboxLayer.Terrain: HitboxConfig.ShowTerrain = state; break;
                case HitboxLayer.Trigger: HitboxConfig.ShowTrigger = state; break;
                case HitboxLayer.Hazard: HitboxConfig.ShowHazard = state; break;
                case HitboxLayer.Breakable: HitboxConfig.ShowBreakable = state; break;
                case HitboxLayer.Interactive: HitboxConfig.ShowInteractive = state; break;
            }
        }

        private Color GetLayerColor(HitboxLayer layer)
        {
            switch (layer)
            {
                case HitboxLayer.Player: return HitboxConfig.PlayerColor;
                case HitboxLayer.Enemy: return HitboxConfig.EnemyColor;
                case HitboxLayer.Attack: return HitboxConfig.AttackColor;
                case HitboxLayer.Terrain: return HitboxConfig.TerrainColor;
                case HitboxLayer.Trigger: return HitboxConfig.TriggerColor;
                case HitboxLayer.Hazard: return HitboxConfig.HazardColor;
                case HitboxLayer.Breakable: return HitboxConfig.BreakableColor;
                case HitboxLayer.Interactive: return HitboxConfig.InteractiveColor;
                default: return Color.white;
            }
        }

        #endregion
    }
}
