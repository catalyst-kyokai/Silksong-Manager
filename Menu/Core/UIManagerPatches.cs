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
            // Find MainMenuOptions to get proper button container
            var mainMenuOptions = UnityEngine.Object.FindObjectOfType<MainMenuOptions>();
            if (mainMenuOptions == null)
            {
                Plugin.Log.LogWarning("Could not find MainMenuOptions - not in menu scene?");
                return;
            }

            // Use extras button as template (like old MainMenuHook)
            MenuButton templateButton = mainMenuOptions.extrasButton;
            if (templateButton == null)
            {
                templateButton = mainMenuOptions.optionsButton;
            }
            if (templateButton == null)
            {
                Plugin.Log.LogError("Could not find template button to clone");
                return;
            }

            // Clone the button to same parent (main menu buttons container)
            var buttonGO = UnityEngine.Object.Instantiate(templateButton.gameObject, templateButton.transform.parent);
            buttonGO.name = "SSManagerButton";

            // Get MenuButton component
            var menuButton = buttonGO.GetComponent<MenuButton>();
            if (menuButton == null)
            {
                Plugin.Log.LogError("Cloned button has no MenuButton component");
                UnityEngine.Object.Destroy(buttonGO);
                return;
            }

            // Remove EventTrigger - may trigger original menu!
            var eventTrigger = buttonGO.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (eventTrigger != null)
            {
                UnityEngine.Object.DestroyImmediate(eventTrigger);
            }

            // Setup button click via EventTrigger
            eventTrigger = buttonGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.Submit
            };
            entry.callback.AddListener((data) => OnSSManagerButtonPressed());
            eventTrigger.triggers.Add(entry);

            menuButton.buttonType = MenuButton.MenuButtonType.Activate;

            // Set button text
            var textObj = buttonGO.GetComponentInChildren<Text>();
            if (textObj != null)
            {
                textObj.text = "SS Manager";

                // Remove localization
                var localize = textObj.GetComponent<AutoLocalizeTextUI>();
                if (localize != null)
                {
                    localize.enabled = false;
                }
            }

            // Position before Quit (last) button
            var quitTransform = mainMenuOptions.quitButton?.transform;
            if (quitTransform != null)
            {
                int quitIndex = quitTransform.GetSiblingIndex();
                buttonGO.transform.SetSiblingIndex(quitIndex);
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
