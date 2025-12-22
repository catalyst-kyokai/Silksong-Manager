using System;
using System.Collections;
using System.Collections.Generic;
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
            Plugin.Log.LogInfo("Modifying cloned menu screen content - AGGRESSIVE CLEANUP...");

            // STEP 1: Hide ALL direct children first, we'll selectively enable what we need
            List<GameObject> childrenToKeep = new List<GameObject>();
            List<GameObject> childrenToHide = new List<GameObject>();

            foreach (Transform child in screenObj.transform)
            {
                var childName = child.name.ToLower();

                // Keep: fleurs (decorative), back button area
                if (childName.Contains("fleur") || childName.Contains("bottom"))
                {
                    childrenToKeep.Add(child.gameObject);
                    Plugin.Log.LogInfo($"Keeping: {child.name}");
                }
                else
                {
                    childrenToHide.Add(child.gameObject);
                    Plugin.Log.LogInfo($"Will hide: {child.name}");
                }
            }

            // Hide children that we don't need
            foreach (var child in childrenToHide)
            {
                child.SetActive(false);
            }

            // STEP 2: Find and configure back button
            if (_modMenuScreen.backButton != null)
            {
                _modMenuScreen.backButton.OnSubmitPressed = new UnityEvent();
                _modMenuScreen.backButton.OnSubmitPressed.AddListener(OnBackButtonPressed);
                // Make sure back button's parent is visible
                _modMenuScreen.backButton.gameObject.SetActive(true);
                var parent = _modMenuScreen.backButton.transform.parent;
                while (parent != null && parent != screenObj.transform)
                {
                    parent.gameObject.SetActive(true);
                    parent = parent.parent;
                }
                Plugin.Log.LogInfo("Configured back button");
            }

            // STEP 3: Create our own title by finding any TMPro text in hidden children that looks like a title
            TMPro.TextMeshProUGUI titleText = null;
            foreach (var hiddenChild in childrenToHide)
            {
                var tmpTexts = hiddenChild.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (var tmp in tmpTexts)
                {
                    if (tmp.gameObject.name.ToLower().Contains("title") ||
                        tmp.transform.parent?.name.ToLower().Contains("title") == true)
                    {
                        titleText = tmp;
                        break;
                    }
                }
                if (titleText != null) break;
            }

            if (titleText != null)
            {
                // Enable this title and its parents
                titleText.gameObject.SetActive(true);
                var parent = titleText.transform.parent;
                while (parent != null && parent != screenObj.transform)
                {
                    parent.gameObject.SetActive(true);
                    parent = parent.parent;
                }

                // Destroy ALL localization components
                DestroyLocalization(titleText.gameObject);
                DestroyLocalization(titleText.transform.parent?.gameObject);

                titleText.text = "Silksong Manager";
                Plugin.Log.LogInfo($"Set title to 'Silksong Manager' on {titleText.gameObject.name}");
            }
            else
            {
                Plugin.Log.LogWarning("Could not find title text element!");
            }

            // STEP 4: Create Keybinds button by cloning back button
            if (_modMenuScreen.backButton != null)
            {
                var keybindsButtonObj = Object.Instantiate(_modMenuScreen.backButton.gameObject, screenObj.transform);
                keybindsButtonObj.name = "KeybindsButton";

                // Position it in the center of the screen
                var rect = keybindsButtonObj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(0, 50); // Center, slightly above middle
                }

                var keybindsButton = keybindsButtonObj.GetComponent<MenuButton>();
                if (keybindsButton != null)
                {
                    keybindsButton.OnSubmitPressed = new UnityEvent();
                    keybindsButton.OnSubmitPressed.AddListener(OnKeybindsButtonPressed);

                    // Set text
                    DestroyLocalization(keybindsButtonObj);
                    SetButtonTextDirect(keybindsButtonObj, "Keybinds");

                    // Setup navigation
                    var keybindsNav = keybindsButton.navigation;
                    keybindsNav.mode = Navigation.Mode.Explicit;
                    keybindsNav.selectOnDown = _modMenuScreen.backButton;
                    keybindsButton.navigation = keybindsNav;

                    var backNav = _modMenuScreen.backButton.navigation;
                    backNav.selectOnUp = keybindsButton;
                    _modMenuScreen.backButton.navigation = backNav;

                    // Set as default highlight
                    _modMenuScreen.defaultHighlight = keybindsButton;

                    Plugin.Log.LogInfo("Created Keybinds button from back button clone");
                }
            }
            else
            {
                Plugin.Log.LogWarning("Back button not found, cannot create Keybinds button!");
            }

            Plugin.Log.LogInfo("Menu screen content modification complete!");
        }

        private static void DestroyLocalization(GameObject obj)
        {
            if (obj == null) return;

            var components = obj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var typeName = comp.GetType().Name;
                if (typeName.Contains("Locali") || typeName.Contains("Translat") ||
                    typeName.Contains("LocalizedText") || typeName.Contains("LocalisedText"))
                {
                    Plugin.Log.LogInfo($"Destroying localization component: {typeName}");
                    Object.DestroyImmediate(comp);
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
