using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace SilksongManager.Menu
{
    /// <summary>
    /// Hooks into the main menu to add the SS Manager button and menu screen.
    /// </summary>
    public static class MainMenuHook
    {
        private static bool _initialized = false;
        private static GameObject _ssManagerButton;
        private static MenuScreen _modMenuScreen;
        private static bool _isInModMenu = false;

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

                CreateModMenuScreen();
                CreateSSManagerButton(mainMenuOptions);

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
            _isInModMenu = false;
        }

        private static void CreateSSManagerButton(MainMenuOptions mainMenuOptions)
        {
            // Find the extras button to clone (it has the right style)
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

            // Change the button text and DISABLE localization
            SetButtonText(_ssManagerButton, "SS Manager");

            // Position the button BETWEEN Extras and Quit
            PositionButtonCorrectly(mainMenuOptions, menuButton);

            Plugin.Log.LogInfo("SS Manager button created successfully!");
        }

        private static void SetButtonText(GameObject buttonObj, string text)
        {
            // Find and disable any LocalizedTextMesh or similar localization components
            var localizers = buttonObj.GetComponentsInChildren<MonoBehaviour>();
            foreach (var loc in localizers)
            {
                // Check for localization components by name
                var typeName = loc.GetType().Name;
                if (typeName.Contains("Locali") || typeName.Contains("Translat"))
                {
                    loc.enabled = false;
                    Plugin.Log.LogInfo($"Disabled localization component: {typeName}");
                }
            }

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

        private static void PositionButtonCorrectly(MainMenuOptions mainMenuOptions, MenuButton ssManagerButton)
        {
            // Get buttons as references
            var extrasButton = mainMenuOptions.extrasButton;
            var quitButton = mainMenuOptions.quitButton;

            if (extrasButton == null || quitButton == null)
            {
                Plugin.Log.LogWarning("Could not find extras or quit button for positioning");
                return;
            }

            // Get RectTransforms
            var ssRect = ssManagerButton.GetComponent<RectTransform>();
            var extrasRect = extrasButton.GetComponent<RectTransform>();
            var quitRect = quitButton.GetComponent<RectTransform>();

            if (ssRect == null || extrasRect == null || quitRect == null)
            {
                Plugin.Log.LogWarning("Could not get RectTransforms for positioning");
                return;
            }

            // Calculate the spacing between buttons (Extras is above Quit, so Extras.y > Quit.y)
            float spacing = extrasRect.anchoredPosition.y - quitRect.anchoredPosition.y;

            // Position SS Manager between Extras and Quit:
            // SS Manager takes position between Extras and Quit
            float ssManagerY = extrasRect.anchoredPosition.y - spacing;
            ssRect.anchoredPosition = new Vector2(extrasRect.anchoredPosition.x, ssManagerY);

            // Move Quit button down by the same spacing to make room
            quitRect.anchoredPosition = new Vector2(quitRect.anchoredPosition.x, quitRect.anchoredPosition.y - spacing);

            // IMPORTANT: Set sibling index to place button in correct visual order
            // Get quit button's sibling index and place our button just before it
            int quitSiblingIndex = quitButton.transform.GetSiblingIndex();
            ssManagerButton.transform.SetSiblingIndex(quitSiblingIndex);

            // Setup navigation: Extras -> SSManager -> Quit
            SetupNavigation(extrasButton, ssManagerButton, quitButton);

            Plugin.Log.LogInfo($"Button positioned at Y={ssRect.anchoredPosition.y}, Quit moved to Y={quitRect.anchoredPosition.y}, Sibling index={quitSiblingIndex}");
        }

        private static void SetupNavigation(MenuButton extrasButton, MenuButton ssManagerButton, MenuButton quitButton)
        {
            // Extras: down goes to SS Manager
            var extrasNav = extrasButton.navigation;
            extrasNav.selectOnDown = ssManagerButton;
            extrasButton.navigation = extrasNav;

            // SS Manager: up goes to Extras, down goes to Quit
            var ssManagerNav = ssManagerButton.navigation;
            ssManagerNav.mode = Navigation.Mode.Explicit;
            ssManagerNav.selectOnUp = extrasButton;
            ssManagerNav.selectOnDown = quitButton;
            ssManagerButton.navigation = ssManagerNav;

            // Quit: up goes to SS Manager
            var quitNav = quitButton.navigation;
            quitNav.selectOnUp = ssManagerButton;
            quitButton.navigation = quitNav;
        }

        private static void OnSSManagerButtonPressed()
        {
            Plugin.Log.LogInfo("SS Manager button pressed!");

            // Use UIManager to transition to our menu screen
            var ui = UIManager.instance;
            if (ui != null && _modMenuScreen != null)
            {
                ui.StartCoroutine(GoToModMenu(ui));
            }
            else
            {
                Plugin.Log.LogWarning("UIManager or ModMenuScreen not available!");
            }
        }

        private static IEnumerator GoToModMenu(UIManager ui)
        {
            _isInModMenu = true;

            // Stop UI input during transition
            var ih = GameManager.instance?.inputHandler;
            ih?.StopUIInput();

            // Fade out main menu like other menus do
            ui.StartCoroutine(FadeOutSprite(ui.gameTitle));

            // Try to fade out subtitle (uses PlayMaker, optional)
            try
            {
                var subtitleFSM = ui.GetType().GetField("subtitleFSM")?.GetValue(ui);
                if (subtitleFSM != null)
                {
                    var sendEventMethod = subtitleFSM.GetType().GetMethod("SendEvent", new[] { typeof(string) });
                    sendEventMethod?.Invoke(subtitleFSM, new object[] { "FADE OUT" });
                }
            }
            catch { /* PlayMaker not available */ }

            yield return ui.StartCoroutine(FadeOutCanvasGroup(ui.mainMenuScreen, ui));

            // Show our menu screen
            yield return ui.StartCoroutine(ShowMenu(_modMenuScreen, ui));

            ih?.StartUIInput();
        }

        public static IEnumerator ReturnToMainMenu(UIManager ui)
        {
            _isInModMenu = false;

            var ih = GameManager.instance?.inputHandler;
            ih?.StopUIInput();

            // Hide our menu
            yield return ui.StartCoroutine(HideMenu(_modMenuScreen, ui));

            // Show main menu
            yield return ui.StartCoroutine(FadeInCanvasGroup(ui.mainMenuScreen, ui));

            // Fade in title
            ui.StartCoroutine(FadeInSprite(ui.gameTitle));

            // Try to fade in subtitle (uses PlayMaker, optional)
            try
            {
                var subtitleFSM = ui.GetType().GetField("subtitleFSM")?.GetValue(ui);
                if (subtitleFSM != null)
                {
                    var sendEventMethod = subtitleFSM.GetType().GetMethod("SendEvent", new[] { typeof(string) });
                    sendEventMethod?.Invoke(subtitleFSM, new object[] { "FADE IN" });
                }
            }
            catch { /* PlayMaker not available */ }

            ih?.StartUIInput();
        }

        public static bool IsInModMenu => _isInModMenu;

        #region UI Transition Helpers (mimicking UIManager methods)

        private static IEnumerator FadeOutSprite(SpriteRenderer sprite)
        {
            if (sprite == null) yield break;

            float alpha = sprite.color.a;
            while (alpha > 0.05f)
            {
                alpha -= Time.unscaledDeltaTime * 3.2f;
                sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, alpha);
                yield return null;
            }
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0f);
        }

        private static IEnumerator FadeInSprite(SpriteRenderer sprite)
        {
            if (sprite == null) yield break;

            float alpha = sprite.color.a;
            while (alpha < 0.95f)
            {
                alpha += Time.unscaledDeltaTime * 3.2f;
                sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, alpha);
                yield return null;
            }
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1f);
        }

        private static IEnumerator FadeOutCanvasGroup(CanvasGroup cg, UIManager ui)
        {
            if (cg == null) yield break;

            float loopFailsafe = 0f;
            while (cg.alpha > 0.05f)
            {
                cg.alpha -= Time.unscaledDeltaTime * ui.MENU_FADE_SPEED;
                loopFailsafe += Time.unscaledDeltaTime;
                if (loopFailsafe >= 2f) break;
                yield return null;
            }
            cg.alpha = 0f;
            cg.interactable = false;
            cg.gameObject.SetActive(false);
        }

        private static IEnumerator FadeInCanvasGroup(CanvasGroup cg, UIManager ui)
        {
            if (cg == null) yield break;

            cg.gameObject.SetActive(true);
            cg.alpha = 0f;
            float loopFailsafe = 0f;

            while (cg.alpha < 0.95f)
            {
                cg.alpha += Time.unscaledDeltaTime * ui.MENU_FADE_SPEED;
                loopFailsafe += Time.unscaledDeltaTime;
                if (loopFailsafe >= 2f) break;
                yield return null;
            }
            cg.alpha = 1f;
            cg.interactable = true;
        }

        private static IEnumerator ShowMenu(MenuScreen menu, UIManager ui)
        {
            if (menu == null) yield break;

            var cg = menu.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                yield return ui.StartCoroutine(FadeInCanvasGroup(cg, ui));
            }
            else
            {
                menu.gameObject.SetActive(true);
            }

            // Highlight default button
            if (menu.HighlightBehaviour == MenuScreen.HighlightDefaultBehaviours.AfterFade)
            {
                menu.HighlightDefault();
            }
        }

        private static IEnumerator HideMenu(MenuScreen menu, UIManager ui)
        {
            if (menu == null) yield break;

            var cg = menu.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                yield return ui.StartCoroutine(FadeOutCanvasGroup(cg, ui));
            }
            else
            {
                menu.gameObject.SetActive(false);
            }
        }

        #endregion

        private static void CreateModMenuScreen()
        {
            // Find an existing MenuScreen to clone as template
            var ui = UIManager.instance;
            if (ui == null)
            {
                Plugin.Log.LogError("UIManager not found!");
                return;
            }

            // Clone extrasMenuScreen as our template
            MenuScreen templateScreen = ui.extrasMenuScreen ?? ui.optionsMenuScreen;
            if (templateScreen == null)
            {
                Plugin.Log.LogError("Could not find template MenuScreen to clone!");
                return;
            }

            // Clone the menu screen
            var screenObj = Object.Instantiate(templateScreen.gameObject, templateScreen.transform.parent);
            screenObj.name = "SSManagerMenuScreen";

            _modMenuScreen = screenObj.GetComponent<MenuScreen>();
            if (_modMenuScreen == null)
            {
                Plugin.Log.LogError("Cloned screen doesn't have MenuScreen component!");
                Object.Destroy(screenObj);
                return;
            }

            // Start hidden
            screenObj.SetActive(false);
            var cg = screenObj.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
            }

            // Modify the cloned screen content
            ModifyMenuScreenContent(screenObj);

            Plugin.Log.LogInfo("Mod menu screen created successfully!");
        }

        private static void ModifyMenuScreenContent(GameObject screenObj)
        {
            Plugin.Log.LogInfo("Modifying cloned menu screen content...");

            // Step 1: Find all TMPro texts and change/hide them
            var tmpTexts = screenObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var text in tmpTexts)
            {
                var objName = text.gameObject.name.ToLower();

                // Change title
                if (objName.Contains("title"))
                {
                    DisableLocalization(text.gameObject);
                    text.text = "SS Manager";
                    Plugin.Log.LogInfo($"Changed title: {text.gameObject.name}");
                }
                // Hide any other text that isn't part of a button
                else if (!objName.Contains("button") && !objName.Contains("back"))
                {
                    var parentButton = text.GetComponentInParent<MenuButton>();
                    if (parentButton == null)
                    {
                        text.gameObject.SetActive(false);
                        Plugin.Log.LogInfo($"Hid text: {text.gameObject.name}");
                    }
                }
            }

            // Step 2: Get all buttons
            var buttons = screenObj.GetComponentsInChildren<MenuButton>(true);
            Plugin.Log.LogInfo($"Found {buttons.Length} buttons in cloned screen");

            MenuButton keybindsButton = null;

            foreach (var button in buttons)
            {
                // Skip the back button - just reconfigure it
                if (button == _modMenuScreen.backButton)
                {
                    button.OnSubmitPressed = new UnityEvent();
                    button.OnSubmitPressed.AddListener(OnBackButtonPressed);
                    Plugin.Log.LogInfo("Configured back button");
                    continue;
                }

                // Use first non-back button as Keybinds, disable all others
                if (keybindsButton == null)
                {
                    keybindsButton = button;
                    DisableLocalization(button.gameObject);
                    SetButtonTextDirect(button.gameObject, "Keybinds");
                    button.OnSubmitPressed = new UnityEvent();
                    button.OnSubmitPressed.AddListener(OnKeybindsButtonPressed);
                    Plugin.Log.LogInfo($"Configured Keybinds button: {button.gameObject.name}");
                }
                else
                {
                    // Disable ALL other buttons completely
                    button.gameObject.SetActive(false);
                    Plugin.Log.LogInfo($"Disabled button: {button.gameObject.name}");
                }
            }

            // Step 3: Hide any remaining UI elements that look like extras content
            // Look for common extras menu elements
            foreach (Transform child in screenObj.transform)
            {
                var childName = child.name.ToLower();

                // Keep essential menu elements
                if (childName.Contains("fleur") || childName.Contains("back") ||
                    childName.Contains("title") || childName.Contains("canvas"))
                {
                    continue;
                }

                // Check if this is a content container
                var hasMenuButton = child.GetComponentInChildren<MenuButton>(true);
                if (hasMenuButton != null)
                {
                    // Keep containers with our configured button or back button
                    var isKeybindsContainer = keybindsButton != null &&
                        (child == keybindsButton.transform || child.IsChildOf(keybindsButton.transform) ||
                         keybindsButton.transform.IsChildOf(child));
                    var isBackContainer = _modMenuScreen.backButton != null &&
                        (child == _modMenuScreen.backButton.transform ||
                         child.IsChildOf(_modMenuScreen.backButton.transform) ||
                         _modMenuScreen.backButton.transform.IsChildOf(child));

                    if (!isKeybindsContainer && !isBackContainer)
                    {
                        // Check each button in this container
                        var childButtons = child.GetComponentsInChildren<MenuButton>(true);
                        bool hasRelevantButton = false;
                        foreach (var cb in childButtons)
                        {
                            if (cb == keybindsButton || cb == _modMenuScreen.backButton)
                            {
                                hasRelevantButton = true;
                                break;
                            }
                        }
                        if (!hasRelevantButton)
                        {
                            child.gameObject.SetActive(false);
                            Plugin.Log.LogInfo($"Disabled container: {child.name}");
                        }
                    }
                }
            }

            // Step 4: If back button not found in modMenuScreen.backButton, configure it manually
            if (_modMenuScreen.backButton == null)
            {
                // Try to find a button that looks like back button
                foreach (var button in buttons)
                {
                    var btnName = button.gameObject.name.ToLower();
                    if (btnName.Contains("back"))
                    {
                        button.OnSubmitPressed = new UnityEvent();
                        button.OnSubmitPressed.AddListener(OnBackButtonPressed);
                        Plugin.Log.LogInfo("Configured back button (found by name)");
                        break;
                    }
                }
            }
        }

        private static void DisableLocalization(GameObject obj)
        {
            var components = obj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var comp in components)
            {
                var typeName = comp.GetType().Name;
                if (typeName.Contains("Locali") || typeName.Contains("Translat") || typeName.Contains("LocalisedText"))
                {
                    comp.enabled = false;
                }
            }
        }

        private static void SetButtonTextDirect(GameObject buttonObj, string text)
        {
            var textComp = buttonObj.GetComponentInChildren<Text>(true);
            if (textComp != null)
            {
                textComp.text = text;
            }

            var tmpComp = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmpComp != null)
            {
                tmpComp.text = text;
            }
        }

        private static void OnKeybindsButtonPressed()
        {
            Plugin.Log.LogInfo("Keybinds button pressed - not yet implemented.");
        }

        private static void OnBackButtonPressed()
        {
            Plugin.Log.LogInfo("Back button pressed, returning to main menu.");

            var ui = UIManager.instance;
            if (ui != null)
            {
                ui.StartCoroutine(ReturnToMainMenu(ui));
            }
        }
    }
}
