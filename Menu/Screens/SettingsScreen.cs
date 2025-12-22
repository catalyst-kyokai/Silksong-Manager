using System;
using UnityEngine;
using UnityEngine.UI;
using SilksongManager.Menu.Core;
using SilksongManager.DebugMenu;

namespace SilksongManager.Menu.Screens
{
    /// <summary>
    /// Settings screen for SS Manager configuration using game-style UI elements.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class SettingsScreen : CustomMenuScreen
    {
        public SettingsScreen() : base("Settings")
        {
        }

        protected override void BuildContent()
        {
            // Add vertical layout for proper spacing
            var vlg = ContentPane.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.padding = new RectOffset(0, 0, 20, 20);

            // === Debug Menu Settings ===

            // Menu Opacity slider
            var opacitySlider = MenuTemplates.CreateSlider(
                "Menu Opacity",
                0.3f, 1.0f,
                DebugMenuConfig.FullMenuOpacity,
                (value) => DebugMenuConfig.FullMenuOpacity = value,
                (v) => $"{Mathf.RoundToInt(v * 100)}%"
            );
            if (opacitySlider != null)
                opacitySlider.transform.SetParent(ContentPane.transform, false);

            // Pause game on menu toggle
            var pauseToggle = MenuTemplates.CreateToggle(
                "Pause Game on Debug Menu",
                DebugMenuConfig.PauseGameOnMenu,
                (value) => DebugMenuConfig.PauseGameOnMenu = value
            );
            if (pauseToggle != null)
                pauseToggle.transform.SetParent(ContentPane.transform, false);

            // === Cheats Settings ===

            // Infinite Health toggle
            var infiniteHealthToggle = MenuTemplates.CreateToggle(
                "Infinite Health",
                Player.CheatSystem.InfiniteHealth,
                (value) => Player.CheatSystem.SetInfiniteHealth(value)
            );
            if (infiniteHealthToggle != null)
                infiniteHealthToggle.transform.SetParent(ContentPane.transform, false);

            // Infinite Silk toggle
            var infiniteSilkToggle = MenuTemplates.CreateToggle(
                "Infinite Silk",
                Player.CheatSystem.InfiniteSilk,
                (value) => Player.CheatSystem.SetInfiniteSilk(value)
            );
            if (infiniteSilkToggle != null)
                infiniteSilkToggle.transform.SetParent(ContentPane.transform, false);

            // Infinite Jumps toggle
            var infiniteJumpsToggle = MenuTemplates.CreateToggle(
                "Infinite Jumps",
                Player.CheatSystem.InfiniteJumps,
                (value) => Player.CheatSystem.SetInfiniteJumps(value)
            );
            if (infiniteJumpsToggle != null)
                infiniteJumpsToggle.transform.SetParent(ContentPane.transform, false);

            // Noclip toggle  
            var noclipToggle = MenuTemplates.CreateToggle(
                "Noclip Mode",
                Player.CheatSystem.NoclipEnabled,
                (value) => Player.CheatSystem.SetNoclip(value)
            );
            if (noclipToggle != null)
                noclipToggle.transform.SetParent(ContentPane.transform, false);

            // === Damage Settings ===

            // Nail Damage multiplier slider
            var nailDamageSlider = MenuTemplates.CreateSlider(
                "Nail Damage Multiplier",
                0.1f, 10.0f,
                Damage.DamageSystem.GetMultiplier(Damage.DamageType.Nail),
                (value) => Damage.DamageSystem.SetMultiplier(Damage.DamageType.Nail, value),
                (v) => $"{v:F1}x"
            );
            if (nailDamageSlider != null)
                nailDamageSlider.transform.SetParent(ContentPane.transform, false);
        }

        protected override void OnScreenShow(NavigationType navType)
        {
            Plugin.Log.LogInfo("Settings screen shown");
        }
    }
}
