using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SilksongManager.Menu.Keybinds;
using SilksongManager.Inventory;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Inventory window with Currency, Crests, Tools, and Abilities tabs.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class InventoryWindow : BaseWindow
    {
        #region Window Properties

        public override int WindowId => 10005;
        public override string Title => "Inventory";
        protected override Vector2 DefaultSize => new Vector2(320, 500);

        #endregion

        #region Private Fields

        /// <summary>Current tab index.</summary>
        private int _currentTab = 0;
        private readonly string[] _tabNames = { "Currency", "Crests", "Tools", "Abilities" };

        // Currency fields
        private int _geoAmount = 1000;
        private int _shardsAmount = 5;

        // Crests fields
        private List<CrestInfo> _crests = new List<CrestInfo>();
        private string[] _crestNames = new string[0];
        private int _selectedCrestIndex = 0;
        private bool _crestsNeedRefresh = true;
        private bool _crestDropdownOpen = false;

        // Tools fields
        private List<Tools.ToolInfo> _tools = new List<Tools.ToolInfo>();
        private string[] _toolNames = new string[0];
        private int _selectedToolIndex = 0;
        private bool _toolsNeedRefresh = true;
        private bool _toolDropdownOpen = false;

        // Abilities fields
        private List<AbilityInfo> _abilities = new List<AbilityInfo>();
        private string[] _abilityNames = new string[0];
        private int _selectedAbilityIndex = 0;
        private bool _abilitiesNeedRefresh = true;
        private bool _abilityDropdownOpen = false;

        // Scroll positions
        private Vector2 _scrollPos = Vector2.zero;

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

            // Draw tab bar
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _tabNames.Length; i++)
            {
                bool isSelected = _currentTab == i;
                if (DebugMenuStyles.DrawToggleButton(_tabNames[i], isSelected, 70f))
                {
                    if (_currentTab != i)
                    {
                        // Close any open dropdowns when switching tabs
                        _crestDropdownOpen = false;
                        _toolDropdownOpen = false;
                        _abilityDropdownOpen = false;
                    }
                    _currentTab = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Draw current tab content
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            switch (_currentTab)
            {
                case 0:
                    DrawCurrencyTab(pd);
                    break;
                case 1:
                    DrawCrestsTab(pd);
                    break;
                case 2:
                    DrawToolsTab(pd);
                    break;
                case 3:
                    DrawAbilitiesTab(pd);
                    break;
            }

            GUILayout.EndScrollView();
        }

        #endregion

        #region Currency Tab

        private void DrawCurrencyTab(PlayerData pd)
        {
            DebugMenuStyles.DrawSectionHeader("GEO");

            GUILayout.Label($"Current: {pd.geo}", DebugMenuStyles.Label);

            // Quick add buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+100", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.AddGeo(100);
            if (GUILayout.Button("+1K", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.AddGeo(1000);
            if (GUILayout.Button("+10K", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.AddGeo(10000);
            DrawKeybindHint(ModAction.AddGeo);
            GUILayout.EndHorizontal();

            // Quick remove buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-100", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.TakeGeo(100);
            if (GUILayout.Button("-1K", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.TakeGeo(1000);
            if (GUILayout.Button("-10K", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.TakeGeo(10000);
            GUILayout.EndHorizontal();

            // Custom amount
            GUILayout.BeginHorizontal();
            GUILayout.Label("Amount:", DebugMenuStyles.Label, GUILayout.Width(55));
            var geoStr = GUILayout.TextField(_geoAmount.ToString(), DebugMenuStyles.TextField, GUILayout.Width(60));
            if (int.TryParse(geoStr, out int g)) _geoAmount = Mathf.Clamp(g, 0, 999999);
            if (GUILayout.Button("Add", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.AddGeo(_geoAmount);
            if (GUILayout.Button("Take", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.TakeGeo(_geoAmount);
            if (GUILayout.Button("Set", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.SetGeo(_geoAmount);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            DebugMenuStyles.DrawSectionHeader("SHELL SHARDS");

            GUILayout.Label($"Current: {pd.ShellShards}", DebugMenuStyles.Label);

            // Quick add buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.AddShards(1);
            if (GUILayout.Button("+5", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.AddShards(5);
            if (GUILayout.Button("+10", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.AddShards(10);
            DrawKeybindHint(ModAction.AddShellShards);
            GUILayout.EndHorizontal();

            // Quick remove buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-1", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.TakeShards(1);
            if (GUILayout.Button("-5", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.TakeShards(5);
            if (GUILayout.Button("-10", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.TakeShards(10);
            GUILayout.EndHorizontal();

            // Custom amount
            GUILayout.BeginHorizontal();
            GUILayout.Label("Amount:", DebugMenuStyles.Label, GUILayout.Width(55));
            var shardsStr = GUILayout.TextField(_shardsAmount.ToString(), DebugMenuStyles.TextField, GUILayout.Width(60));
            if (int.TryParse(shardsStr, out int s)) _shardsAmount = Mathf.Clamp(s, 0, 9999);
            if (GUILayout.Button("Add", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.AddShards(_shardsAmount);
            if (GUILayout.Button("Take", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.TakeShards(_shardsAmount);
            if (GUILayout.Button("Set", DebugMenuStyles.ButtonSmall))
                Currency.CurrencyActions.SetShards(_shardsAmount);
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Crests Tab

        private void DrawCrestsTab(PlayerData pd)
        {
            DebugMenuStyles.DrawSectionHeader("CREST SELECTION");

            // Refresh crests list
            if (_crestsNeedRefresh || _crests.Count == 0)
            {
                _crests = CrestActions.GetAllCrests();
                _crestNames = _crests.Select(c => c.Name).ToArray();
                _crestsNeedRefresh = false;
                if (_selectedCrestIndex >= _crests.Count) _selectedCrestIndex = 0;
            }

            // Current equipped crest
            var equippedCrest = CrestActions.GetEquippedCrestName();
            GUILayout.Label($"Equipped: {equippedCrest}", DebugMenuStyles.Label);

            GUILayout.Space(5);

            if (_crests.Count > 0)
            {
                // Dropdown button
                GUILayout.BeginHorizontal();
                GUILayout.Label("Select:", DebugMenuStyles.Label, GUILayout.Width(50));

                string currentName = _selectedCrestIndex < _crestNames.Length ? _crestNames[_selectedCrestIndex] : "---";
                string dropdownLabel = _crestDropdownOpen ? $"▲ {currentName}" : $"▼ {currentName}";

                if (GUILayout.Button(dropdownLabel, DebugMenuStyles.ButtonSmall))
                {
                    _crestDropdownOpen = !_crestDropdownOpen;
                }
                GUILayout.EndHorizontal();

                // Dropdown list
                if (_crestDropdownOpen)
                {
                    GUILayout.BeginVertical(DebugMenuStyles.Box);
                    for (int i = 0; i < _crestNames.Length; i++)
                    {
                        bool isSelected = i == _selectedCrestIndex;
                        var style = isSelected ? DebugMenuStyles.ToggleOn : DebugMenuStyles.ButtonSmall;

                        string itemLabel = _crestNames[i];
                        if (_crests[i].IsUnlocked) itemLabel += " ✓";
                        if (_crests[i].IsEquipped) itemLabel += " [E]";

                        if (GUILayout.Button(itemLabel, style))
                        {
                            _selectedCrestIndex = i;
                            _crestDropdownOpen = false;
                        }
                    }
                    GUILayout.EndVertical();
                }

                // Show status of selected
                if (!_crestDropdownOpen && _selectedCrestIndex < _crests.Count)
                {
                    var selected = _crests[_selectedCrestIndex];
                    string status = selected.IsUnlocked ? "✓ Unlocked" : "✗ Locked";
                    if (selected.IsEquipped) status += " (Equipped)";
                    GUILayout.Label(status, DebugMenuStyles.Label);

                    // Actions for selected crest
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Unlock", DebugMenuStyles.ButtonSmall))
                    {
                        CrestActions.UnlockCrest(selected.Name);
                        _crestsNeedRefresh = true;
                    }
                    if (GUILayout.Button("Lock/Reset", DebugMenuStyles.ButtonSmall))
                    {
                        CrestActions.LockCrest(selected.Name);
                        _crestsNeedRefresh = true;
                    }
                    if (GUILayout.Button("Equip", DebugMenuStyles.ButtonSmall))
                    {
                        CrestActions.EquipCrest(selected.Name);
                        _crestsNeedRefresh = true;
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(10);

            DebugMenuStyles.DrawSectionHeader("ALL CRESTS");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock All", DebugMenuStyles.Button))
            {
                CrestActions.UnlockAllCrests();
                _crestsNeedRefresh = true;
            }
            if (GUILayout.Button("Reset All", DebugMenuStyles.Button))
            {
                CrestActions.ResetAllCrests();
                _crestsNeedRefresh = true;
            }
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Tools Tab

        private void DrawToolsTab(PlayerData pd)
        {
            DebugMenuStyles.DrawSectionHeader("TOOL SELECTION");

            // Refresh tools list
            if (_toolsNeedRefresh || _tools.Count == 0)
            {
                _tools = Tools.ToolActions.GetNonCrestTools();
                _toolNames = _tools.Select(t => t.Name).ToArray();
                _toolsNeedRefresh = false;
                if (_selectedToolIndex >= _tools.Count) _selectedToolIndex = 0;
            }

            if (_tools.Count > 0)
            {
                // Dropdown button
                GUILayout.BeginHorizontal();
                GUILayout.Label("Select:", DebugMenuStyles.Label, GUILayout.Width(50));

                string currentName = _selectedToolIndex < _toolNames.Length ? _toolNames[_selectedToolIndex] : "---";
                if (currentName.Length > 20) currentName = currentName.Substring(0, 17) + "...";
                string dropdownLabel = _toolDropdownOpen ? $"▲ {currentName}" : $"▼ {currentName}";

                if (GUILayout.Button(dropdownLabel, DebugMenuStyles.ButtonSmall))
                {
                    _toolDropdownOpen = !_toolDropdownOpen;
                }
                GUILayout.EndHorizontal();

                // Dropdown list
                if (_toolDropdownOpen)
                {
                    GUILayout.BeginVertical(DebugMenuStyles.Box);
                    for (int i = 0; i < _toolNames.Length; i++)
                    {
                        bool isSelected = i == _selectedToolIndex;
                        var style = isSelected ? DebugMenuStyles.ToggleOn : DebugMenuStyles.ButtonSmall;

                        string itemLabel = _toolNames[i];
                        if (itemLabel.Length > 25) itemLabel = itemLabel.Substring(0, 22) + "...";

                        // Get live unlock status from game
                        var tool = ToolItemManager.GetToolByName(_tools[i].Name);
                        bool isUnlocked = tool != null && tool.IsUnlockedNotHidden;
                        if (isUnlocked) itemLabel += " ✓";

                        if (GUILayout.Button(itemLabel, style))
                        {
                            _selectedToolIndex = i;
                            _toolDropdownOpen = false;
                        }
                    }
                    GUILayout.EndVertical();
                }

                // Show status of selected
                if (!_toolDropdownOpen && _selectedToolIndex < _tools.Count)
                {
                    GUILayout.Label($"Full: {_tools[_selectedToolIndex].Name}", DebugMenuStyles.LabelSmall);

                    // Get live status from game
                    var selectedTool = ToolItemManager.GetToolByName(_tools[_selectedToolIndex].Name);
                    bool isUnlocked = selectedTool != null && selectedTool.IsUnlockedNotHidden;
                    string status = isUnlocked ? "✓ Unlocked" : "✗ Locked";
                    GUILayout.Label(status, DebugMenuStyles.Label);

                    // Actions for selected tool
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Unlock", DebugMenuStyles.ButtonSmall))
                    {
                        Tools.ToolActions.UnlockTool(_tools[_selectedToolIndex].Name);
                        _toolsNeedRefresh = true;
                    }
                    if (GUILayout.Button("Lock", DebugMenuStyles.ButtonSmall))
                    {
                        Tools.ToolActions.LockTool(_tools[_selectedToolIndex].Name);
                        _toolsNeedRefresh = true;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No tools available", DebugMenuStyles.Label);
            }

            GUILayout.Space(10);

            DebugMenuStyles.DrawSectionHeader("ALL TOOLS");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock All", DebugMenuStyles.Button))
            {
                Tools.ToolActions.UnlockAllTools();
                _toolsNeedRefresh = true;
            }
            if (GUILayout.Button("Lock All", DebugMenuStyles.Button))
            {
                Tools.ToolActions.LockAllTools();
                _toolsNeedRefresh = true;
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Replenish All", DebugMenuStyles.Button))
            {
                Tools.ToolActions.ReplenishAllTools();
            }
        }

        #endregion

        #region Abilities Tab

        private void DrawAbilitiesTab(PlayerData pd)
        {
            DebugMenuStyles.DrawSectionHeader("ABILITY SELECTION");

            // Refresh abilities list
            if (_abilitiesNeedRefresh || _abilities.Count == 0)
            {
                _abilities = AbilityActions.GetAllAbilities();
                _abilityNames = _abilities.Select(a => a.Name).ToArray();
                _abilitiesNeedRefresh = false;
                if (_selectedAbilityIndex >= _abilities.Count) _selectedAbilityIndex = 0;
            }

            if (_abilities.Count > 0)
            {
                // Dropdown button
                GUILayout.BeginHorizontal();
                GUILayout.Label("Select:", DebugMenuStyles.Label, GUILayout.Width(50));

                string currentName = _selectedAbilityIndex < _abilityNames.Length ? _abilityNames[_selectedAbilityIndex] : "---";
                string dropdownLabel = _abilityDropdownOpen ? $"▲ {currentName}" : $"▼ {currentName}";

                if (GUILayout.Button(dropdownLabel, DebugMenuStyles.ButtonSmall))
                {
                    _abilityDropdownOpen = !_abilityDropdownOpen;
                }
                GUILayout.EndHorizontal();

                // Dropdown list
                if (_abilityDropdownOpen)
                {
                    GUILayout.BeginVertical(DebugMenuStyles.Box);
                    for (int i = 0; i < _abilityNames.Length; i++)
                    {
                        bool isSelected = i == _selectedAbilityIndex;
                        var style = isSelected ? DebugMenuStyles.ToggleOn : DebugMenuStyles.ButtonSmall;

                        string itemLabel = _abilityNames[i];
                        if (_abilities[i].IsUnlocked) itemLabel += " ✓";

                        if (GUILayout.Button(itemLabel, style))
                        {
                            _selectedAbilityIndex = i;
                            _abilityDropdownOpen = false;
                        }
                    }
                    GUILayout.EndVertical();
                }

                // Show status of selected
                if (!_abilityDropdownOpen && _selectedAbilityIndex < _abilities.Count)
                {
                    var selected = _abilities[_selectedAbilityIndex];
                    string status = selected.IsUnlocked ? "✓ Unlocked" : "✗ Locked";
                    GUILayout.Label(status, DebugMenuStyles.Label);

                    // Actions for selected ability
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Grant", DebugMenuStyles.ButtonSmall))
                    {
                        AbilityActions.GrantAbility(selected.Ability);
                        _abilitiesNeedRefresh = true;
                    }
                    if (GUILayout.Button("Revoke", DebugMenuStyles.ButtonSmall))
                    {
                        AbilityActions.RevokeAbility(selected.Ability);
                        _abilitiesNeedRefresh = true;
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(10);

            DebugMenuStyles.DrawSectionHeader("ALL ABILITIES");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Grant All", DebugMenuStyles.Button))
            {
                AbilityActions.GrantAllAbilities();
                _abilitiesNeedRefresh = true;
            }
            if (GUILayout.Button("Revoke All", DebugMenuStyles.Button))
            {
                AbilityActions.RevokeAllAbilities();
                _abilitiesNeedRefresh = true;
            }
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Helpers

        private void DrawKeybindHint(ModAction action)
        {
            var key = ModKeybindManager.GetKeybind(action);
            if (key != KeyCode.None)
            {
                GUILayout.Label($"[{DebugMenuStyles.KeyCodeToString(key)}]", DebugMenuStyles.Label, GUILayout.Width(45));
            }
        }

        #endregion
    }
}
