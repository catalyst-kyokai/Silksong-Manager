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
        /// Postfix on UIManager.Awake to initialize templates and start button addition.
        /// </summary>
        [HarmonyPatch(typeof(UIManager), "Awake")]
        [HarmonyPostfix]
        public static void UIManager_Awake_Postfix(UIManager __instance)
        {
            try
            {
                // Initialize template system
                MenuTemplates.Initialize(__instance);

                // SS Manager button is created by MainMenuHook, not here
                // __instance.StartCoroutine(WaitAndAddSSManagerButton(__instance));

                Plugin.Log.LogInfo("UIManager hooked successfully");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"UIManager_Awake_Postfix error: {e.Message}");
            }
        }

        private static System.Collections.IEnumerator WaitAndAddSSManagerButton(UIManager uiManager)
        {
            float timeout = 15f;
            float elapsed = 0f;

            // Wait for MainMenuOptions to exist
            while (elapsed < timeout)
            {
                var mainMenuOptions = UnityEngine.Object.FindObjectOfType<MainMenuOptions>();
                if (mainMenuOptions != null)
                {
                    // Wait one more frame for everything to settle
                    yield return null;
                    AddSSManagerButton(uiManager);
                    yield break;
                }

                yield return new UnityEngine.WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            Plugin.Log.LogWarning("Timeout waiting for MainMenuOptions");
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

            // Setup button click via MenuButton.OnSubmitPressed (like old MainMenuHook)
            menuButton.OnSubmitPressed = new UnityEngine.Events.UnityEvent();
            menuButton.OnSubmitPressed.AddListener(OnSSManagerButtonPressed);
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

            // Add custom credits text
            AddCustomCredits();
        }

        private static void AddCustomCredits()
        {
            var texts = UnityEngine.Object.FindObjectsOfType<Text>();
            foreach (var text in texts)
            {
                // Version string typically looks like "1.0.xxxxx"
                if (text.text.Contains("1.0.") && text.text.Length < 20)
                {
                    Plugin.Log.LogInfo($"Found version text: {text.text}");

                    var creditsObj = UnityEngine.Object.Instantiate(text.gameObject, text.transform.parent);
                    creditsObj.name = "SSManagerCredits";

                    // Remove any existing components that might interfere
                    foreach (var comp in creditsObj.GetComponents<MonoBehaviour>())
                    {
                        if (comp is not Text)
                        {
                            UnityEngine.Object.Destroy(comp);
                        }
                    }

                    var creditsText = creditsObj.GetComponent<Text>();
                    creditsText.text = "Silksong Manager Edition\n<size=16>with <color=#ff6060>❤</color> by Catalyst</size>";
                    creditsText.lineSpacing = 0.8f;
                    creditsText.supportRichText = true;

                    // Check for Layout Group
                    var layoutGroup = text.transform.parent.GetComponent<VerticalLayoutGroup>();
                    if (layoutGroup != null)
                    {
                        creditsObj.transform.SetSiblingIndex(text.transform.GetSiblingIndex() + 1);
                        Plugin.Log.LogInfo("Added credits via LayoutGroup");
                    }
                    else
                    {
                        // Manual positioning
                        var rect = creditsObj.GetComponent<RectTransform>();
                        var originalRect = text.GetComponent<RectTransform>();
                        rect.anchoredPosition = originalRect.anchoredPosition - new UnityEngine.Vector2(0, 45);
                        Plugin.Log.LogInfo("Added credits via manual positioning");
                    }
                    return;
                }
            }
            Plugin.Log.LogInfo("Could not find version text to attach credits to.");
        }

        private static void OnSSManagerButtonPressed()
        {
            Plugin.Log.LogInfo("SS Manager button pressed!");

            // Create screen lazily
            if (_ssManagerScreen == null || _ssManagerScreen.Container == null)
            {
                _ssManagerScreen = new Screens.SSManagerScreen();
            }

            MenuNavigation.Show(_ssManagerScreen);
        }
    }
}
