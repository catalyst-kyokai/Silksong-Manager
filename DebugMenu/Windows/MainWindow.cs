using UnityEngine;
using SilksongManager.Menu.Keybinds;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Main debug menu window - central hub for accessing all features.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class MainWindow : BaseWindow
    {
        #region Window Properties

        public override int WindowId => 10001;
        public override string Title => "Silksong Manager";
        protected override Vector2 DefaultSize => new Vector2(320, 420);

        #endregion

        #region Private Fields

        /// <summary>Reference to the debug menu controller.</summary>
        private DebugMenuController _controller;

        #endregion

        #region Constructor

        public MainWindow(DebugMenuController controller) : base()
        {
            _controller = controller;
        }

        #endregion

        #region Drawing Methods

        protected override void DrawContent()
        {
            DrawStatusSection();

            GUILayout.Space(8);

            DrawQuickActions();

            GUILayout.Space(8);

            DrawWindowButtons();

            GUILayout.FlexibleSpace();

            DrawFooter();
        }

        private void DrawStatusSection()
        {
            DebugMenuStyles.DrawSectionHeader("STATUS");

            var pd = Plugin.PD;
            var hero = Plugin.Hero;

            if (pd != null && hero != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Health: {pd.health}/{pd.maxHealth}", DebugMenuStyles.Label);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Silk: {pd.silk}/{pd.silkMax}", DebugMenuStyles.Label);
                GUILayout.EndHorizontal();

                GUILayout.Label($"Geo: {pd.geo}", DebugMenuStyles.Label);

                GUILayout.Space(4);

                DebugMenuStyles.DrawStatus("Invincibility", pd.isInvincible);
                DebugMenuStyles.DrawStatus("Noclip", Player.CheatSystem.NoclipEnabled);
            }
            else
            {
                GUILayout.Label("Not in game", DebugMenuStyles.LabelCentered);
            }
        }

        private void DrawQuickActions()
        {
            DebugMenuStyles.DrawSectionHeader("QUICK ACTIONS");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Heal", DebugMenuStyles.Button))
            {
                Player.PlayerActions.QuickHeal();
            }

            if (GUILayout.Button("Max Silk", DebugMenuStyles.Button))
            {
                Player.PlayerActions.QuickSilk();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            var pd = Plugin.PD;
            bool isInvincible = pd?.isInvincible ?? false;
            if (DebugMenuStyles.DrawToggleButton(isInvincible ? "Invincible ✓" : "Invincible", isInvincible))
            {
                Player.PlayerActions.ToggleInvincibility();
            }

            bool isNoclip = Player.CheatSystem.NoclipEnabled;
            if (DebugMenuStyles.DrawToggleButton(isNoclip ? "Noclip ✓" : "Noclip", isNoclip))
            {
                Player.CheatSystem.ToggleNoclip();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawWindowButtons()
        {
            DebugMenuStyles.DrawSectionHeader("WINDOWS");

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            if (GUILayout.Button("Player", DebugMenuStyles.Button))
            {
                _controller.ToggleWindow<PlayerWindow>();
            }

            if (GUILayout.Button("Enemies", DebugMenuStyles.Button))
            {
                _controller.ToggleWindow<EnemiesWindow>();
            }

            if (GUILayout.Button("Keybinds", DebugMenuStyles.Button))
            {
                _controller.ToggleWindow<KeybindsWindow>();
            }

            if (GUILayout.Button("World", DebugMenuStyles.Button))
            {
                _controller.ToggleWindow<WorldWindow>();
            }

            if (GUILayout.Button("Speed", DebugMenuStyles.Button))
            {
                _controller.ToggleWindow<SpeedControlWindow>();
            }

            if (GUILayout.Button("Items", DebugMenuStyles.Button))
            {
                _controller.ToggleWindow<ItemsWindow>();
            }

            GUILayout.EndVertical();
            GUILayout.BeginVertical();

            if (GUILayout.Button("Settings", DebugMenuStyles.Button))
            {
                _controller.ToggleWindow<SettingsWindow>();
            }

            if (GUILayout.Button("Debug Info", DebugMenuStyles.Button))
            {
                _controller.ToggleWindow<DebugInfoWindow>();
            }

            if (GUILayout.Button("Combat", DebugMenuStyles.Button))
            {
                _controller.ToggleWindow<CombatWindow>();
            }

            if (GUILayout.Button("Hitboxes", DebugMenuStyles.Button))
            {
                _controller.ToggleWindow<HitboxWindow>();
            }

            if (GUILayout.Button("Save States", DebugMenuStyles.Button))
            {
                _controller.ToggleWindow<SaveStateWindow>();
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            DebugMenuStyles.DrawSeparator();

            GUILayout.BeginHorizontal();

            var toggleKey = ModKeybindManager.GetKeybind(ModAction.ToggleDebugMenu);
            GUILayout.Label($"Press [{DebugMenuStyles.KeyCodeToString(toggleKey)}] to close",
                DebugMenuStyles.LabelCentered);

            GUILayout.EndHorizontal();
        }

        protected override void DrawHeader()
        {
            GUILayout.Label(Title, DebugMenuStyles.Header);
            DebugMenuStyles.DrawSeparator();
        }

        #endregion
    }
}
