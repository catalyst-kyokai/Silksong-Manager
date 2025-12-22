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
        public override int WindowId => 10002;
        public override string Title => "Player";
        protected override Vector2 DefaultSize => new Vector2(280, 350);

        private int _healthInput = 10;
        private int _silkInput = 10;

        protected override void DrawContent()
        {
            var pd = Plugin.PD;
            var hero = Plugin.Hero;

            if (pd == null || hero == null)
            {
                GUILayout.Label("Not in game", DebugMenuStyles.LabelCentered);
                return;
            }

            // Health section
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

            // Silk section
            DebugMenuStyles.DrawSectionHeader("SILK");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Current: {pd.silk}/{pd.silkMax}", DebugMenuStyles.Label);
            if (GUILayout.Button("Max", DebugMenuStyles.ButtonSmall, GUILayout.Width(50)))
            {
                Player.PlayerActions.QuickSilk();
            }
            GUILayout.EndHorizontal();

            // Toggles section
            DebugMenuStyles.DrawSectionHeader("TOGGLES");

            // Invincibility
            GUILayout.BeginHorizontal();
            bool isInvincible = pd.isInvincible;
            if (DebugMenuStyles.DrawToggleButton(isInvincible ? "Invincibility ✓" : "Invincibility", isInvincible))
            {
                Player.PlayerActions.ToggleInvincibility();
            }
            DrawKeybindHint(ModAction.ToggleInvincibility);
            GUILayout.EndHorizontal();

            // Noclip - use CheatSystem
            GUILayout.BeginHorizontal();
            bool isNoclip = Player.CheatSystem.NoclipEnabled;
            if (DebugMenuStyles.DrawToggleButton(isNoclip ? "Noclip ✓" : "Noclip", isNoclip))
            {
                Player.CheatSystem.ToggleNoclip();
            }
            DrawKeybindHint(ModAction.ToggleNoclip);
            GUILayout.EndHorizontal();

            // Infinite Jumps - use CheatSystem
            GUILayout.BeginHorizontal();
            bool infiniteJumps = Player.CheatSystem.InfiniteJumps;
            if (DebugMenuStyles.DrawToggleButton(infiniteJumps ? "Infinite Jumps ✓" : "Infinite Jumps", infiniteJumps))
            {
                Player.CheatSystem.ToggleInfiniteJumps();
            }
            DrawKeybindHint(ModAction.ToggleInfiniteJumps);
            GUILayout.EndHorizontal();

            // Infinite Health - use CheatSystem
            GUILayout.BeginHorizontal();
            bool infiniteHealth = Player.CheatSystem.InfiniteHealth;
            if (DebugMenuStyles.DrawToggleButton(infiniteHealth ? "Infinite Health ✓" : "Infinite Health", infiniteHealth))
            {
                Player.CheatSystem.ToggleInfiniteHealth();
            }
            DrawKeybindHint(ModAction.ToggleInfiniteHealth);
            GUILayout.EndHorizontal();

            // Infinite Silk - use CheatSystem
            GUILayout.BeginHorizontal();
            bool infiniteSilk = Player.CheatSystem.InfiniteSilk;
            if (DebugMenuStyles.DrawToggleButton(infiniteSilk ? "Infinite Silk ✓" : "Infinite Silk", infiniteSilk))
            {
                Player.CheatSystem.ToggleInfiniteSilk();
            }
            DrawKeybindHint(ModAction.ToggleInfiniteSilk);
            GUILayout.EndHorizontal();
        }

        private void DrawKeybindHint(ModAction action)
        {
            var key = ModKeybindManager.GetKeybind(action);
            if (key != KeyCode.None)
            {
                GUILayout.Label($"[{DebugMenuStyles.KeyCodeToString(key)}]", DebugMenuStyles.Label, GUILayout.Width(60));
            }
        }
    }
}
