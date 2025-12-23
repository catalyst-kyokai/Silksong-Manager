using UnityEngine;
using SilksongManager.Menu.Keybinds;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Items and currency window.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class ItemsWindow : BaseWindow
    {
        #region Window Properties

        public override int WindowId => 10005;
        public override string Title => "Items";
        protected override Vector2 DefaultSize => new Vector2(280, 350);

        #endregion

        #region Private Fields

        /// <summary>Custom geo amount input.</summary>
        private int _geoAmount = 1000;
        /// <summary>Custom shards amount input.</summary>
        private int _shardsAmount = 5;

        #endregion

        #region Drawing Methods

        protected override void DrawContent()
        {
            var pd = Plugin.PD;

            if (pd == null)
            {
                GUILayout.Label("Not in game", DebugMenuStyles.LabelCentered);
                return;
            }

            DebugMenuStyles.DrawSectionHeader("GEO");

            GUILayout.Label($"Current: {pd.geo}", DebugMenuStyles.Label);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("+100", DebugMenuStyles.ButtonSmall))
            {
                Currency.CurrencyActions.AddGeo(100);
            }
            if (GUILayout.Button("+1000", DebugMenuStyles.ButtonSmall))
            {
                Currency.CurrencyActions.AddGeo(1000);
            }
            if (GUILayout.Button("+10000", DebugMenuStyles.ButtonSmall))
            {
                Currency.CurrencyActions.AddGeo(10000);
            }
            DrawKeybindHint(ModAction.AddGeo);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Custom:", DebugMenuStyles.Label, GUILayout.Width(60));
            var geoStr = GUILayout.TextField(_geoAmount.ToString(), DebugMenuStyles.TextField, GUILayout.Width(60));
            if (int.TryParse(geoStr, out int g)) _geoAmount = Mathf.Clamp(g, 0, 999999);
            if (GUILayout.Button("Add", DebugMenuStyles.ButtonSmall))
            {
                Currency.CurrencyActions.AddGeo(_geoAmount);
            }
            GUILayout.EndHorizontal();

            DebugMenuStyles.DrawSectionHeader("SHELL SHARDS");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("+1", DebugMenuStyles.ButtonSmall))
            {
                Currency.CurrencyActions.AddShards(1);
            }
            if (GUILayout.Button("+5", DebugMenuStyles.ButtonSmall))
            {
                Currency.CurrencyActions.AddShards(5);
            }
            if (GUILayout.Button("+10", DebugMenuStyles.ButtonSmall))
            {
                Currency.CurrencyActions.AddShards(10);
            }
            DrawKeybindHint(ModAction.AddShellShards);

            GUILayout.EndHorizontal();

            DebugMenuStyles.DrawSectionHeader("TOOLS");

            if (GUILayout.Button("Unlock All Tools", DebugMenuStyles.Button))
            {
                Tools.ToolActions.UnlockAllTools();
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
