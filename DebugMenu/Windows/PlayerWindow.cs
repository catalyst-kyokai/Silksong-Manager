using UnityEngine;
using SilksongManager.Menu.Keybinds;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Player-related actions window.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class PlayerWindow : BaseWindow
    {
        #region Window Properties

        public override int WindowId => 10002;
        public override string Title => "Player";
        protected override Vector2 DefaultSize => new Vector2(280, 420);

        #endregion

        #region Private Fields

        /// <summary>Health value input.</summary>
        private int _healthInput = 10;
        /// <summary>Silk value input (unused, kept for future).</summary>
        private int _silkInput = 10;
        /// <summary>Noclip normal speed input.</summary>
        private string _noclipSpeedInput = "15";
        /// <summary>Noclip boost speed input.</summary>
        private string _noclipBoostInput = "30";

        #endregion

        #region Drawing Methods

        protected override void DrawContent()
        {
            var pd = Plugin.PD;
            var hero = Plugin.Hero;

            if (pd == null || hero == null)
            {
                GUILayout.Label("Not in game", DebugMenuStyles.LabelCentered);
                return;
            }

            DebugMenuStyles.DrawSectionHeader("HEALTH");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Current: {pd.health}/{pd.maxHealth}", DebugMenuStyles.Label);
            if (GUILayout.Button("Full", DebugMenuStyles.ButtonSmall, GUILayout.Width(50)))
            {
                Player.PlayerActions.QuickHeal();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Set:", DebugMenuStyles.Label, GUILayout.Width(40));
            var healthStr = GUILayout.TextField(_healthInput.ToString(), DebugMenuStyles.TextField, GUILayout.Width(50));
            if (int.TryParse(healthStr, out int h)) _healthInput = Mathf.Clamp(h, 1, 100);
            if (GUILayout.Button("Apply", DebugMenuStyles.ButtonSmall))
            {
                Player.PlayerActions.SetHealth(_healthInput);
            }
            GUILayout.EndHorizontal();

            DebugMenuStyles.DrawSectionHeader("SILK");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Current: {pd.silk}/{pd.silkMax}", DebugMenuStyles.Label);
            if (GUILayout.Button("Max", DebugMenuStyles.ButtonSmall, GUILayout.Width(50)))
            {
                Player.PlayerActions.QuickSilk();
            }
            GUILayout.EndHorizontal();

            DebugMenuStyles.DrawSectionHeader("TOGGLES");

            GUILayout.BeginHorizontal();
            bool isInvincible = Player.CheatSystem.UserInvincible;
            if (DebugMenuStyles.DrawToggleButton(isInvincible ? "Invincibility ✓" : "Invincibility", isInvincible))
            {
                Player.PlayerActions.ToggleInvincibility();
            }
            DrawKeybindHint(ModAction.ToggleInvincibility);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool isNoclip = Player.CheatSystem.NoclipEnabled;
            if (DebugMenuStyles.DrawToggleButton(isNoclip ? "Noclip ✓" : "Noclip", isNoclip))
            {
                Player.CheatSystem.ToggleNoclip();
            }
            DrawKeybindHint(ModAction.ToggleNoclip);
            GUILayout.EndHorizontal();

            // Noclip Speed Settings (only show when noclip is enabled or always for discoverability)
            if (isNoclip)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("  Speed:", DebugMenuStyles.Label, GUILayout.Width(55));
                string newSpeedInput = GUILayout.TextField(_noclipSpeedInput, DebugMenuStyles.TextField, GUILayout.Width(40));
                if (newSpeedInput != _noclipSpeedInput)
                {
                    _noclipSpeedInput = newSpeedInput;
                    if (float.TryParse(newSpeedInput, out float val) && val > 0)
                        Player.CheatSystem.NoclipSpeed = val;
                }
                GUILayout.Label("  Boost:", DebugMenuStyles.Label, GUILayout.Width(45));
                string newBoostInput = GUILayout.TextField(_noclipBoostInput, DebugMenuStyles.TextField, GUILayout.Width(40));
                if (newBoostInput != _noclipBoostInput)
                {
                    _noclipBoostInput = newBoostInput;
                    if (float.TryParse(newBoostInput, out float val) && val > 0)
                        Player.CheatSystem.NoclipBoostSpeed = val;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            bool infiniteJumps = Player.CheatSystem.InfiniteJumps;
            if (DebugMenuStyles.DrawToggleButton(infiniteJumps ? "Infinite Jumps ✓" : "Infinite Jumps", infiniteJumps))
            {
                Player.CheatSystem.ToggleInfiniteJumps();
            }
            DrawKeybindHint(ModAction.ToggleInfiniteJumps);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool infiniteHealth = Player.CheatSystem.InfiniteHealth;
            if (DebugMenuStyles.DrawToggleButton(infiniteHealth ? "Infinite Health ✓" : "Infinite Health", infiniteHealth))
            {
                Player.CheatSystem.ToggleInfiniteHealth();
            }
            DrawKeybindHint(ModAction.ToggleInfiniteHealth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool infiniteSilk = Player.CheatSystem.InfiniteSilk;
            if (DebugMenuStyles.DrawToggleButton(infiniteSilk ? "Infinite Silk ✓" : "Infinite Silk", infiniteSilk))
            {
                Player.CheatSystem.ToggleInfiniteSilk();
            }
            DrawKeybindHint(ModAction.ToggleInfiniteSilk);
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Helpers

        private void DrawKeybindHint(ModAction action)
        {
            var key = ModKeybindManager.GetKeybind(action);
            if (key != KeyCode.None)
            {
                GUILayout.Label($"[{DebugMenuStyles.KeyCodeToString(key)}]", DebugMenuStyles.Label, GUILayout.Width(60));
            }
        }

        #endregion
    }
}
