using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace SilksongManager.Menu
{
    /// <summary>
    /// Hooks into the main menu to add the SS Manager button.
    /// </summary>
    public static class MainMenuHook
    {
        private static bool _initialized = false;
        private static GameObject _ssManagerButton;
        private static ModMenuScreen _modMenuScreen;

        /// <summary>
        /// Initialize the main menu hook. Called when menu scene loads.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                var mainMenuOptions = Object.FindObjectOfType<MainMenuOptions>();
                if (mainMenuOptions == null)
                {
                    Plugin.Log.LogWarning("MainMenuOptions not found - not in menu scene?");
                    return;
                }

                CreateSSManagerButton(mainMenuOptions);
                CreateModMenuScreen();
                _initialized = true;
                Plugin.Log.LogInfo("Main menu hook initialized successfully!");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to initialize main menu hook: {e}");
            }
        }

        /// <summary>
        /// Reset the hook when leaving menu scene.
        /// </summary>
        public static void Reset()
        {
            _initialized = false;
            _ssManagerButton = null;
            _modMenuScreen = null;
        }

        private static void CreateSSManagerButton(MainMenuOptions mainMenuOptions)
        {
            // Find a button to clone - prefer extrasButton, fallback to optionsButton
            MenuButton templateButton = mainMenuOptions.extrasButton;
            if (templateButton == null)
            {
                templateButton = mainMenuOptions.optionsButton;
            }

            if (templateButton == null)
            {
                Plugin.Log.LogError("Could not find template button to clone!");
                return;
            }

            // Clone the button
            _ssManagerButton = Object.Instantiate(templateButton.gameObject, templateButton.transform.parent);
            _ssManagerButton.name = "SSManagerButton";

            // Get the MenuButton component
            var menuButton = _ssManagerButton.GetComponent<MenuButton>();
            if (menuButton == null)
            {
                Plugin.Log.LogError("Cloned button doesn't have MenuButton component!");
                Object.Destroy(_ssManagerButton);
                return;
            }

            // Clear existing events and add our handler
            menuButton.OnSubmitPressed = new UnityEvent();
            menuButton.OnSubmitPressed.AddListener(OnSSManagerButtonPressed);

            // Change the button text
            SetButtonText(_ssManagerButton, "SS Manager");

            // Position the button after the last visible button (before quit)
            PositionButton(mainMenuOptions, menuButton);

            Plugin.Log.LogInfo("SS Manager button created successfully!");
        }

        private static void SetButtonText(GameObject buttonObj, string text)
        {
            // Try to find Text component in children
            var textComponent = buttonObj.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = text;
                return;
            }

            // Try TMPro if available
            var tmpComponent = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpComponent != null)
            {
                tmpComponent.text = text;
                return;
            }

            Plugin.Log.LogWarning("Could not find text component on button!");
        }

        private static void PositionButton(MainMenuOptions mainMenuOptions, MenuButton ssManagerButton)
        {
            // Get the quitButton as reference for positioning
            var quitButton = mainMenuOptions.quitButton;
            var extrasButton = mainMenuOptions.extrasButton;

            if (quitButton == null)
            {
                Plugin.Log.LogWarning("Quit button not found for positioning reference");
                return;
            }

            // Get RectTransforms
            var ssRect = ssManagerButton.GetComponent<RectTransform>();
            var quitRect = quitButton.GetComponent<RectTransform>();
            var templateRect = (extrasButton ?? mainMenuOptions.optionsButton).GetComponent<RectTransform>();

            if (ssRect == null || quitRect == null || templateRect == null)
            {
                Plugin.Log.LogWarning("Could not get RectTransforms for positioning");
                return;
            }

            // Calculate vertical spacing between buttons
            float spacing = 0f;
            if (extrasButton != null && mainMenuOptions.optionsButton != null)
            {
                var extrasRect = extrasButton.GetComponent<RectTransform>();
                var optionsRect = mainMenuOptions.optionsButton.GetComponent<RectTransform>();
                spacing = optionsRect.anchoredPosition.y - extrasRect.anchoredPosition.y;
            }
            else
            {
                spacing = 60f; // Default spacing
            }

            // Position our button above the quit button
            var quitPos = quitRect.anchoredPosition;
            ssRect.anchoredPosition = new Vector2(quitPos.x, quitPos.y + spacing);

            // Move quit button down
            quitRect.anchoredPosition = new Vector2(quitPos.x, quitPos.y);

            // Actually, let's insert between extras and quit
            // Move quit button down to make room
            quitRect.anchoredPosition = new Vector2(quitPos.x, quitPos.y - spacing);

            // Setup navigation
            SetupNavigation(mainMenuOptions, ssManagerButton);
        }

        private static void SetupNavigation(MainMenuOptions mainMenuOptions, MenuButton ssManagerButton)
        {
            // Get references
            var quitButton = mainMenuOptions.quitButton;
            var extrasButton = mainMenuOptions.extrasButton ?? mainMenuOptions.achievementsButton ?? mainMenuOptions.optionsButton;

            if (extrasButton == null || quitButton == null) return;

            // Setup navigation: Extras -> SSManager -> Quit
            var extrasNav = extrasButton.navigation;
            var ssManagerNav = ssManagerButton.navigation;
            var quitNav = quitButton.navigation;

            // Extras points down to SSManager
            extrasNav.selectOnDown = ssManagerButton;
            extrasButton.navigation = extrasNav;

            // SSManager: up to Extras, down to Quit
            ssManagerNav.mode = Navigation.Mode.Explicit;
            ssManagerNav.selectOnUp = extrasButton;
            ssManagerNav.selectOnDown = quitButton;
            ssManagerButton.navigation = ssManagerNav;

            // Quit points up to SSManager
            quitNav.selectOnUp = ssManagerButton;
            quitButton.navigation = quitNav;
        }

        private static void OnSSManagerButtonPressed()
        {
            Plugin.Log.LogInfo("SS Manager button pressed!");
            
            if (_modMenuScreen != null)
            {
                _modMenuScreen.Show();
            }
            else
            {
                Plugin.Log.LogWarning("ModMenuScreen not initialized!");
            }
        }

        private static void CreateModMenuScreen()
        {
            // Create a GameObject for the mod menu screen
            var screenObj = new GameObject("SSManagerMenuScreen");
            Object.DontDestroyOnLoad(screenObj);
            
            _modMenuScreen = screenObj.AddComponent<ModMenuScreen>();
            _modMenuScreen.Initialize();
        }
    }
}
