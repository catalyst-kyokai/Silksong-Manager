using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using SilksongManager.Menu.Keybinds;
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
        private static ModMenuController _menuController;
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
            _menuController = null;
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

            // IMPORTANT: Remove EventTrigger if present - it may trigger original menu!
            var eventTrigger = _ssManagerButton.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (eventTrigger != null)
            {
                Object.DestroyImmediate(eventTrigger);
                Plugin.Log.LogInfo("Removed EventTrigger from cloned button");
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
            Plugin.Log.LogInfo("GoToModMenu started");
            _isInModMenu = true;

            // Stop UI input during transition
            var ih = GameManager.instance?.inputHandler;
            ih?.StopUIInput();

            // IMPORTANT: Make sure original ExtrasMenuScreen is hidden!
            if (ui.extrasMenuScreen != null)
            {
                var origCg = ui.extrasMenuScreen.GetComponent<CanvasGroup>();
                if (origCg != null)
                {
                    origCg.alpha = 0f;
                    origCg.interactable = false;
                    origCg.blocksRaycasts = false;
                }
                ui.extrasMenuScreen.gameObject.SetActive(false);
                Plugin.Log.LogInfo("Explicitly hid original ExtrasMenuScreen");
            }

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
            Plugin.Log.LogInfo("Showing mod menu screen");
            yield return ui.StartCoroutine(ShowMenu(_modMenuScreen, ui));

            // Activate our input controller
            if (_menuController != null)
            {
                _menuController.SetActive(true);
            }

            ih?.StartUIInput();
            Plugin.Log.LogInfo("GoToModMenu completed");
        }

        /// <summary>
        /// Called by ModMenuController when Escape is pressed, or by back button.
        /// </summary>
        public static void HandleBackPressed()
        {
            if (!_isInModMenu) return;

            Plugin.Log.LogInfo("HandleBackPressed called");

            // Deactivate controller
            if (_menuController != null)
            {
                _menuController.SetActive(false);
            }

            var ui = UIManager.instance;
            if (ui != null)
            {
                ui.StartCoroutine(ReturnToMainMenu(ui));
            }
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

        private static void OnBackButtonPressed()
        {
            Plugin.Log.LogInfo("Back button pressed");
            HandleBackPressed();
        }

        public static bool IsInModMenu => _isInModMenu;

        /// <summary>
        /// Hide main menu elements (for use by sub-screens like Keybinds).
        /// </summary>
        public static void HideMainMenu(UIManager ui)
        {
            ui.StartCoroutine(FadeOutSprite(ui.gameTitle));

            // Fade out subtitle
            try
            {
                var subtitleFSM = ui.GetType().GetField("subtitleFSM")?.GetValue(ui);
                if (subtitleFSM != null)
                {
                    var sendEventMethod = subtitleFSM.GetType().GetMethod("SendEvent", new[] { typeof(string) });
                    sendEventMethod?.Invoke(subtitleFSM, new object[] { "FADE OUT" });
                }
            }
            catch { }

            ui.StartCoroutine(FadeOutCanvasGroup(ui.mainMenuScreen, ui));
        }

        /// <summary>
        /// Show main menu elements (for use by sub-screens returning).
        /// </summary>
        public static void ShowMainMenu(UIManager ui)
        {
            ui.StartCoroutine(FadeInSprite(ui.gameTitle));

            // Fade in subtitle
            try
            {
                var subtitleFSM = ui.GetType().GetField("subtitleFSM")?.GetValue(ui);
                if (subtitleFSM != null)
                {
                    var sendEventMethod = subtitleFSM.GetType().GetMethod("SendEvent", new[] { typeof(string) });
                    sendEventMethod?.Invoke(subtitleFSM, new object[] { "FADE IN" });
                }
            }
            catch { }

            ui.StartCoroutine(FadeInCanvasGroup(ui.mainMenuScreen, ui));
        }

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
            sprite.enabled = false;
        }

        private static IEnumerator FadeInSprite(SpriteRenderer sprite)
        {
            if (sprite == null) yield break;

            sprite.enabled = true;
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

            // Log the template hierarchy for debugging
            Plugin.Log.LogInfo("=== Template MenuScreen hierarchy ===");
            LogHierarchy(templateScreen.transform, 0);
            Plugin.Log.LogInfo("=== End hierarchy ===");

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

            // Add our input controller
            _menuController = screenObj.AddComponent<ModMenuController>();
            Plugin.Log.LogInfo("Added ModMenuController to menu screen");

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

        private static void LogHierarchy(Transform t, int depth)
        {
            string indent = new string(' ', depth * 2);
            var components = t.GetComponents<Component>();
            string compStr = string.Join(",", System.Array.ConvertAll(components, c => c?.GetType().Name ?? "null"));
            Plugin.Log.LogInfo($"{indent}{t.name} [active={t.gameObject.activeSelf}] ({compStr})");

            foreach (Transform child in t)
            {
                LogHierarchy(child, depth + 1);
            }
        }

        private static void ModifyMenuScreenContent(GameObject screenObj)
        {
            Plugin.Log.LogInfo("Modifying cloned menu screen content - DESTROYING unwanted elements...");

            // STEP 0: FIRST - Reparent backButton to screen root BEFORE we destroy its parent!
            MenuButton savedBackButton = null;
            if (_modMenuScreen.backButton != null)
            {
                savedBackButton = _modMenuScreen.backButton;
                savedBackButton.transform.SetParent(screenObj.transform, false);
                savedBackButton.gameObject.SetActive(false); // Hide for now
                Plugin.Log.LogInfo($"Saved backButton from destruction: {savedBackButton.name}");
            }

            // Collect elements to keep vs destroy
            var toDestroy = new List<GameObject>();
            Transform titleTransform = null;
            Transform topFleurTransform = null;

            foreach (Transform child in screenObj.transform)
            {
                // Skip the saved back button
                if (savedBackButton != null && child == savedBackButton.transform)
                {
                    Plugin.Log.LogInfo($"Skipping (saved): {child.name}");
                    continue;
                }

                var childName = child.name.ToLower();

                if (childName.Contains("title"))
                {
                    titleTransform = child;
                    Plugin.Log.LogInfo($"Keeping: {child.name}");
                }
                else if (childName.Contains("fleur"))
                {
                    topFleurTransform = child;
                    Plugin.Log.LogInfo($"Keeping: {child.name}");
                }
                else
                {
                    // DESTROY everything else - Content, Controls, CheatCodeListener, ImportSavePrompt
                    toDestroy.Add(child.gameObject);
                    Plugin.Log.LogInfo($"Will DESTROY: {child.name}");
                }
            }

            // DESTROY unwanted elements
            foreach (var obj in toDestroy)
            {
                Plugin.Log.LogInfo($"Destroying: {obj.name}");
                Object.DestroyImmediate(obj);
            }

            // STEP 2: Modify Title text
            if (titleTransform != null)
            {
                titleTransform.gameObject.SetActive(true);

                // Title uses UI.Text (based on hierarchy log)
                var textComponent = titleTransform.GetComponent<Text>();
                if (textComponent != null)
                {
                    DestroyLocalization(titleTransform.gameObject);
                    textComponent.text = "Silksong Manager";
                    Plugin.Log.LogInfo($"Set title UI.Text to 'Silksong Manager'");
                }
            }
            else
            {
                Plugin.Log.LogWarning("Title element not found!");
            }

            // STEP 3: Enable fleurs
            if (topFleurTransform != null)
            {
                topFleurTransform.gameObject.SetActive(true);
                Plugin.Log.LogInfo($"Enabled TopFleur");
            }

            // STEP 4: Configure and position back button
            if (savedBackButton != null)
            {
                savedBackButton.OnSubmitPressed = new UnityEvent();
                savedBackButton.OnSubmitPressed.AddListener(OnBackButtonPressed);
                savedBackButton.gameObject.SetActive(true);

                // Position at bottom of screen
                var backRect = savedBackButton.GetComponent<RectTransform>();
                if (backRect != null)
                {
                    backRect.anchorMin = new Vector2(0.5f, 0);
                    backRect.anchorMax = new Vector2(0.5f, 0);
                    backRect.pivot = new Vector2(0.5f, 0.5f);
                    backRect.anchoredPosition = new Vector2(0, 80);
                }

                DestroyLocalization(savedBackButton.gameObject);

                Plugin.Log.LogInfo($"Configured back button at bottom of screen");
            }

            // STEP 5: Create Keybinds button by cloning back button
            if (_modMenuScreen.backButton != null)
            {
                var keybindsButtonObj = Object.Instantiate(_modMenuScreen.backButton.gameObject, screenObj.transform);
                keybindsButtonObj.name = "KeybindsButton";

                // Position above the back button
                var rect = keybindsButtonObj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0, 0); // Center of screen
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

                    Plugin.Log.LogInfo("Created Keybinds button at center");
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
            Plugin.Log.LogInfo("Keybinds button pressed!");

            var ui = UIManager.instance;
            if (ui != null)
            {
                ui.StartCoroutine(GoToKeybindsScreen(ui));
            }
        }

        private static IEnumerator GoToKeybindsScreen(UIManager ui)
        {
            var ih = GameManager.instance?.inputHandler;
            ih?.StopUIInput();

            // Deactivate ModMenuController so Escape doesn't trigger HandleBackPressed
            if (_menuController != null)
            {
                Plugin.Log.LogInfo("Deactivating ModMenuController in GoToKeybindsScreen");
                _menuController.SetActive(false);
            }
            else
            {
                Plugin.Log.LogError("_menuController is NULL in GoToKeybindsScreen!");
            }

            // Hide mod menu
            var modCg = _modMenuScreen?.GetComponent<CanvasGroup>();
            yield return ui.StartCoroutine(FadeOutCanvasGroup(modCg, ui));

            // Initialize keybinds screen if needed
            ModKeybindsScreen.Initialize();

            // Show keybinds screen
            yield return ui.StartCoroutine(ModKeybindsScreen.Show(ui));

            ih?.StartUIInput();
        }

        /// <summary>
        /// Called from keybinds screen to return to SS Manager menu.
        /// </summary>
        public static void ReturnFromKeybindsScreen()
        {
            var ui = UIManager.instance;
            if (ui != null)
            {
                ui.StartCoroutine(ReturnFromKeybindsScreenCoroutine(ui));
            }
        }

        private static IEnumerator ReturnFromKeybindsScreenCoroutine(UIManager ui)
        {
            var ih = GameManager.instance?.inputHandler;
            ih?.StopUIInput();

            // Hide keybinds screen
            yield return ui.StartCoroutine(ModKeybindsScreen.Hide(ui));

            // Show mod menu
            var modCg = _modMenuScreen?.GetComponent<CanvasGroup>();
            yield return ui.StartCoroutine(FadeInCanvasGroup(modCg, ui));

            // Reactivate ModMenuController for SS Manager
            if (_menuController != null)
            {
                _menuController.SetActive(true);
            }

            // Focus Keybinds button
            if (_modMenuScreen?.defaultHighlight != null)
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(_modMenuScreen.defaultHighlight.gameObject);
            }

            ih?.StartUIInput();
        }
    }
}
