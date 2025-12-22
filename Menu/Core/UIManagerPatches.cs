using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SilksongManager.Menu.Core
{
    /// <summary>
    /// Harmony patches for UIManager to add SS Manager button and integrate navigation.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class UIManagerPatches
    {
        private static Harmony _harmony;
        private static bool _applied = false;
        private static Screens.SSManagerScreen _ssManagerScreen;

        /// <summary>
        /// Apply all UI patches.
        /// </summary>
        public static void Apply()
        {
            if (_applied) return;

            try
            {
                _harmony = new Harmony("com.catalyst.silksongmanager.ui");
                _harmony.PatchAll(typeof(UIManagerPatches));
                _applied = true;
                Plugin.Log.LogInfo("UIManagerPatches applied");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to apply UIManagerPatches: {e.Message}");
            }
        }

        /// <summary>
        /// Remove all patches.
        /// </summary>
        public static void Remove()
        {
            _harmony?.UnpatchSelf();
            _applied = false;
        }

        /// <summary>
        /// Postfix on UIManager.Awake to initialize templates and add SS Manager button.
        /// </summary>
        [HarmonyPatch(typeof(UIManager), "Awake")]
        [HarmonyPostfix]
        public static void UIManager_Awake_Postfix(UIManager __instance)
        {
            try
            {
                // Initialize template system
                MenuTemplates.Initialize(__instance);

                // Add SS Manager button to options menu
                AddSSManagerButton(__instance);

                Plugin.Log.LogInfo("UIManager hooked successfully");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"UIManager_Awake_Postfix error: {e.Message}");
            }
        }

        /// <summary>
        /// Prefix on UIManager.UIGoBack to handle custom screen navigation.
        /// </summary>
        [HarmonyPatch(typeof(UIManager), "UIGoBack")]
        [HarmonyPrefix]
        public static bool UIManager_UIGoBack_Prefix(ref bool __result)
        {
            if (MenuNavigation.HasHistory)
            {
                MenuNavigation.HandleBackPressed();
                __result = true;
                return false; // Skip original method
            }
            return true; // Continue to original
        }

        /// <summary>
        /// Prefix on UIManager.OnDestroy to cleanup navigation state.
        /// </summary>
        [HarmonyPatch(typeof(UIManager), "OnDestroy")]
        [HarmonyPrefix]
        public static void UIManager_OnDestroy_Prefix(UIManager __instance)
        {
            if (UIManager.instance == __instance)
            {
                MenuNavigation.Reset();
                _ssManagerScreen?.Dispose();
                _ssManagerScreen = null;
            }
        }

        private static void AddSSManagerButton(UIManager uiManager)
        {
            var mainMenuScreen = uiManager.mainMenuScreen;
            if (mainMenuScreen == null)
            {
                Plugin.Log.LogWarning("Could not find mainMenuScreen");
                return;
            }

            // Find content pane in main menu
            var contentPane = MenuTemplates.FindChild(mainMenuScreen.gameObject, "Content");
            if (contentPane == null)
            {
                // Try alternate path
                contentPane = MenuTemplates.FindChild(mainMenuScreen.gameObject, "MenuButtons");
            }
            if (contentPane == null)
            {
                Plugin.Log.LogWarning("Could not find Content pane in main menu screen");
                return;
            }

            // Create SS Manager button
            var button = MenuTemplates.CreateTextButton("SS Manager", OnSSManagerButtonPressed);
            if (button == null)
            {
                Plugin.Log.LogError("Failed to create SS Manager button");
                return;
            }

            // Add to content pane
            button.transform.SetParent(contentPane.transform, false);
            button.SetActive(true);

            // Position it nicely - before Options or near end
            // Try to find Options button index
            int targetIndex = contentPane.transform.childCount - 2; // Before last 2 (usually Options/Quit)
            if (targetIndex >= 0)
            {
                button.transform.SetSiblingIndex(targetIndex);
            }

            Plugin.Log.LogInfo("SS Manager button added to main menu");
        }

        private static void OnSSManagerButtonPressed()
        {
            // Create screen lazily
            if (_ssManagerScreen == null || _ssManagerScreen.Container == null)
            {
                _ssManagerScreen = new Screens.SSManagerScreen();
            }

            MenuNavigation.Show(_ssManagerScreen);
        }
    }
}
