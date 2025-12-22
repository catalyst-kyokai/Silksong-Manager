using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace SilksongManager.Menu.Keybinds
{
    /// <summary>
    /// Builds and manages the mod keybinds menu screen by cloning the actual keyboard menu.
    /// </summary>
    public static class ModKeybindsScreen
    {
        private static MenuScreen _keybindsMenuScreen;
        private static List<ModMappableKeyEntry> _mappableEntries = new List<ModMappableKeyEntry>();
        private static bool _initialized = false;
        private static bool _isActive = false;

        // Pending conflict resolution
        private static ModMappableKeyEntry _pendingEntry;
        private static KeyCode _pendingKeyCode;

        /// <summary>
        /// Initialize by cloning the keyboard menu from the game.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                CreateFromKeyboardMenu();
                _initialized = true;
                Plugin.Log.LogInfo("ModKeybindsScreen initialized from keyboard menu");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to initialize ModKeybindsScreen: {e}");
            }
        }

        public static void Reset()
        {
            _initialized = false;
            _keybindsMenuScreen = null;
            _mappableEntries.Clear();
            _isActive = false;
        }

        public static MenuScreen GetScreen() => _keybindsMenuScreen;
        public static bool IsActive => _isActive;

        private static void CreateFromKeyboardMenu()
        {
            var ui = UIManager.instance;
            if (ui == null)
            {
                Plugin.Log.LogError("UIManager not found!");
                return;
            }

            // Find the keyboard menu container via UIButtonSkins
            var uibs = ui.uiButtonSkins;
            if (uibs == null || uibs.mappableKeyboardButtons == null)
            {
                Plugin.Log.LogError("UIButtonSkins or mappableKeyboardButtons not found!");
                return;
            }

            // The keyboard menu should be a parent of mappableKeyboardButtons
            // Find the MenuScreen in the hierarchy
            var keyboardPanel = uibs.mappableKeyboardButtons;
            MenuScreen templateScreen = null;
            Transform current = keyboardPanel;

            while (current != null && templateScreen == null)
            {
                templateScreen = current.GetComponent<MenuScreen>();
                if (templateScreen == null)
                    current = current.parent;
            }

            if (templateScreen == null)
            {
                // Fallback: use options menu as template
                templateScreen = ui.optionsMenuScreen;
                Plugin.Log.LogWarning("Could not find keyboard MenuScreen, using options menu as fallback");
            }

            // Clone the screen
            var screenObj = Object.Instantiate(templateScreen.gameObject, templateScreen.transform.parent);
            screenObj.name = "ModKeybindsMenuScreen";

            _keybindsMenuScreen = screenObj.GetComponent<MenuScreen>();
            if (_keybindsMenuScreen == null)
            {
                Object.Destroy(screenObj);
                Plugin.Log.LogError("Cloned screen doesn't have MenuScreen!");
                return;
            }

            // Hide initially
            screenObj.SetActive(false);
            var cg = screenObj.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
            }

            // Modify content
            ModifyClonedScreen(screenObj);

            Plugin.Log.LogInfo("Mod keybinds screen created successfully");
        }

        private static void ModifyClonedScreen(GameObject screenObj)
        {
            // First, save backButton
            MenuButton savedBackButton = _keybindsMenuScreen.backButton;
            if (savedBackButton != null)
            {
                savedBackButton.transform.SetParent(screenObj.transform, false);
                savedBackButton.gameObject.SetActive(false);
            }

            // Find and keep title and fleur, destroy everything else
            var toDestroy = new List<GameObject>();
            Transform titleTransform = null;
            Transform topFleurTransform = null;

            foreach (Transform child in screenObj.transform)
            {
                if (savedBackButton != null && child == savedBackButton.transform) continue;

                var name = child.name.ToLower();
                if (name.Contains("title"))
                {
                    titleTransform = child;
                }
                else if (name.Contains("fleur"))
                {
                    topFleurTransform = child;
                }
                else
                {
                    toDestroy.Add(child.gameObject);
                }
            }

            foreach (var obj in toDestroy)
            {
                Object.DestroyImmediate(obj);
            }

            // Set title
            if (titleTransform != null)
            {
                titleTransform.gameObject.SetActive(true);
                DestroyLocalization(titleTransform.gameObject);
                var textComp = titleTransform.GetComponent<Text>();
                if (textComp != null)
                {
                    textComp.text = "Mod Keybinds";
                }
            }

            // Enable fleur
            if (topFleurTransform != null)
            {
                topFleurTransform.gameObject.SetActive(true);
            }

            // Now clone ONE MappableKey from the actual keyboard menu as template
            var uibs = UIManager.instance.uiButtonSkins;
            MappableKey templateKey = null;
            if (uibs != null && uibs.mappableKeyboardButtons != null)
            {
                templateKey = uibs.mappableKeyboardButtons.GetComponentInChildren<MappableKey>(true);
            }

            // Create content container with layout
            var contentObj = CreateContentContainer(screenObj.transform, templateKey);

            // Setup back button
            if (savedBackButton != null)
            {
                savedBackButton.gameObject.SetActive(true);
                savedBackButton.OnSubmitPressed = new UnityEvent();
                savedBackButton.OnSubmitPressed.AddListener(OnBackButtonPressed);
                DestroyLocalization(savedBackButton.gameObject);

                // Position at bottom
                var backRect = savedBackButton.GetComponent<RectTransform>();
                if (backRect != null)
                {
                    backRect.anchorMin = new Vector2(0.5f, 0);
                    backRect.anchorMax = new Vector2(0.5f, 0);
                    backRect.pivot = new Vector2(0.5f, 0.5f);
                    backRect.anchoredPosition = new Vector2(0, 60);
                }

                // Setup navigation
                if (_mappableEntries.Count > 0)
                {
                    var lastEntry = _mappableEntries[_mappableEntries.Count - 1];
                    var backNav = savedBackButton.navigation;
                    backNav.mode = Navigation.Mode.Explicit;
                    backNav.selectOnUp = lastEntry.button;
                    backNav.selectOnDown = _mappableEntries[0].button;
                    savedBackButton.navigation = backNav;
                }
            }

            // Set default highlight
            if (_mappableEntries.Count > 0)
            {
                _keybindsMenuScreen.defaultHighlight = _mappableEntries[0].button;
            }
        }

        private static GameObject CreateContentContainer(Transform parent, MappableKey templateKey)
        {
            // Create scrollable content
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(parent, false);

            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.1f, 0.15f);
            contentRect.anchorMax = new Vector2(0.9f, 0.85f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            // Create two-column layout
            var leftColumn = CreateColumn(contentObj.transform, new Vector2(0, 0.5f), new Vector2(0.48f, 1f));
            var rightColumn = CreateColumn(contentObj.transform, new Vector2(0.52f, 0.5f), new Vector2(1f, 1f));

            // Get all actions
            var actions = (ModAction[])Enum.GetValues(typeof(ModAction));
            int halfCount = (actions.Length + 1) / 2;

            _mappableEntries.Clear();

            for (int i = 0; i < actions.Length; i++)
            {
                var column = i < halfCount ? leftColumn : rightColumn;
                var entry = CreateKeybindEntry(column, actions[i], templateKey);
                _mappableEntries.Add(entry);
            }

            // Setup navigation between entries
            SetupNavigation();

            return contentObj;
        }

        private static Transform CreateColumn(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var colObj = new GameObject("Column");
            colObj.transform.SetParent(parent, false);

            var colRect = colObj.AddComponent<RectTransform>();
            colRect.anchorMin = anchorMin;
            colRect.anchorMax = anchorMax;
            colRect.offsetMin = Vector2.zero;
            colRect.offsetMax = Vector2.zero;

            var layout = colObj.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 20;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(10, 10, 20, 20);

            return colObj.transform;
        }

        private static ModMappableKeyEntry CreateKeybindEntry(Transform parent, ModAction action, MappableKey templateKey)
        {
            var entryObj = new GameObject($"Entry_{action}");
            entryObj.transform.SetParent(parent, false);

            var entryRect = entryObj.AddComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(0, 55);

            // Horizontal layout like game: label  [key]
            var hLayout = entryObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleRight;
            hLayout.spacing = 15;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;

            // Label (left side)
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(entryObj.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(200, 45);
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 200;

            var labelText = labelObj.AddComponent<Text>();
            labelText.font = GetGameFont();
            labelText.fontSize = 26;
            labelText.fontStyle = FontStyle.Normal;
            labelText.alignment = TextAnchor.MiddleRight;
            labelText.color = Color.white;
            labelText.text = ModKeybindManager.GetActionName(action);

            // Key button (right side) - styled like game
            var keyBtnObj = new GameObject("KeyButton");
            keyBtnObj.transform.SetParent(entryObj.transform, false);
            var keyRect = keyBtnObj.AddComponent<RectTransform>();
            keyRect.sizeDelta = new Vector2(70, 45);
            var keyLayout = keyBtnObj.AddComponent<LayoutElement>();
            keyLayout.preferredWidth = 70;
            keyLayout.preferredHeight = 45;

            // Background image (styled like game keys)
            var uibs = UIManager.instance?.uiButtonSkins;
            var keyBg = keyBtnObj.AddComponent<Image>();
            keyBg.sprite = uibs?.squareKey;
            keyBg.color = Color.white;
            keyBg.type = Image.Type.Sliced;

            // Key text - WHITE color like game
            var keyTextObj = new GameObject("Text");
            keyTextObj.transform.SetParent(keyBtnObj.transform, false);
            var keyTextRect = keyTextObj.AddComponent<RectTransform>();
            keyTextRect.anchorMin = Vector2.zero;
            keyTextRect.anchorMax = Vector2.one;
            keyTextRect.offsetMin = Vector2.zero;
            keyTextRect.offsetMax = Vector2.zero;

            var keyText = keyTextObj.AddComponent<Text>();
            keyText.font = GetGameFont();
            keyText.fontSize = 24;
            keyText.fontStyle = FontStyle.Bold;
            keyText.alignment = TextAnchor.MiddleCenter;
            keyText.color = Color.white;  // WHITE text like game

            // Add button component
            var button = keyBtnObj.AddComponent<MenuButton>();
            button.OnSubmitPressed = new UnityEvent();

            var entry = new ModMappableKeyEntry
            {
                action = action,
                button = button,
                keyText = keyText,
                keyBg = keyBg,
                isListening = false
            };

            // Set click handler
            button.OnSubmitPressed.AddListener(() => OnKeyEntryClicked(entry));

            // Display current keybind
            UpdateEntryDisplay(entry);

            return entry;
        }

        private static void SetupNavigation()
        {
            for (int i = 0; i < _mappableEntries.Count; i++)
            {
                var entry = _mappableEntries[i];
                var nav = entry.button.navigation;
                nav.mode = Navigation.Mode.Explicit;

                // Up: previous entry or wrap to back button
                if (i > 0)
                    nav.selectOnUp = _mappableEntries[i - 1].button;
                else if (_keybindsMenuScreen.backButton != null)
                    nav.selectOnUp = _keybindsMenuScreen.backButton;

                // Down: next entry or back button
                if (i < _mappableEntries.Count - 1)
                    nav.selectOnDown = _mappableEntries[i + 1].button;
                else if (_keybindsMenuScreen.backButton != null)
                    nav.selectOnDown = _keybindsMenuScreen.backButton;

                entry.button.navigation = nav;
            }
        }

        private static void OnKeyEntryClicked(ModMappableKeyEntry entry)
        {
            if (entry.isListening) return;

            // Start listening for new key
            entry.isListening = true;
            entry.keyText.text = "...";

            // Start listening coroutine
            UIManager.instance.StartCoroutine(ListenForKey(entry));
        }

        private static IEnumerator ListenForKey(ModMappableKeyEntry entry)
        {
            yield return null;
            yield return null;

            while (entry.isListening)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    entry.isListening = false;
                    UpdateEntryDisplay(entry);
                    yield break;
                }

                foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                {
                    if (key == KeyCode.None || key == KeyCode.Mouse0 || key == KeyCode.Mouse1) continue;
                    if (key == KeyCode.Escape) continue;

                    if (Input.GetKeyDown(key))
                    {
                        entry.isListening = false;
                        HandleKeySelected(entry, key);
                        yield break;
                    }
                }

                yield return null;
            }
        }

        private static void HandleKeySelected(ModMappableKeyEntry entry, KeyCode key)
        {
            // Check conflicts with other mod keybinds
            if (ModKeybindManager.IsModKeybindConflicting(key, entry.action, out ModAction conflicting))
            {
                // For now, just replace (can add dialog later)
                ModKeybindManager.SetKeybind(conflicting, KeyCode.None);
                RefreshAllDisplays();
            }

            // Apply the new keybind
            ModKeybindManager.SetKeybind(entry.action, key);
            UpdateEntryDisplay(entry);
        }

        private static void UpdateEntryDisplay(ModMappableKeyEntry entry)
        {
            var key = ModKeybindManager.GetKeybind(entry.action);
            entry.keyText.text = KeyCodeToShortString(key);

            // Update background sprite based on key type
            var uibs = UIManager.instance?.uiButtonSkins;
            if (uibs != null)
            {
                if (key == KeyCode.None)
                {
                    entry.keyBg.sprite = uibs.blankKey;
                }
                else if (IsWideKey(key))
                {
                    entry.keyBg.sprite = uibs.rectangleKey;
                }
                else
                {
                    entry.keyBg.sprite = uibs.squareKey;
                }
            }
        }

        private static void RefreshAllDisplays()
        {
            foreach (var entry in _mappableEntries)
            {
                UpdateEntryDisplay(entry);
            }
        }

        private static void OnBackButtonPressed()
        {
            Plugin.Log.LogInfo("Back from Keybinds screen");
            MainMenuHook.ReturnFromKeybindsScreen();
        }

        private static Font GetGameFont()
        {
            // Try to get the game's font
            var existingText = Object.FindAnyObjectByType<Text>();
            if (existingText != null)
                return existingText.font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void DestroyLocalization(GameObject obj)
        {
            if (obj == null) return;
            foreach (var comp in obj.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp != null && comp.GetType().Name.Contains("Locali"))
                {
                    Object.DestroyImmediate(comp);
                }
            }
        }

        private static bool IsWideKey(KeyCode key)
        {
            return key == KeyCode.Space || key == KeyCode.Tab || key == KeyCode.Return ||
                   key == KeyCode.LeftShift || key == KeyCode.RightShift ||
                   key == KeyCode.LeftControl || key == KeyCode.RightControl ||
                   key == KeyCode.LeftAlt || key == KeyCode.RightAlt ||
                   key == KeyCode.Backspace;
        }

        private static string KeyCodeToShortString(KeyCode key)
        {
            if (key == KeyCode.None) return "---";

            return key switch
            {
                KeyCode.Alpha0 => "0",
                KeyCode.Alpha1 => "1",
                KeyCode.Alpha2 => "2",
                KeyCode.Alpha3 => "3",
                KeyCode.Alpha4 => "4",
                KeyCode.Alpha5 => "5",
                KeyCode.Alpha6 => "6",
                KeyCode.Alpha7 => "7",
                KeyCode.Alpha8 => "8",
                KeyCode.Alpha9 => "9",
                KeyCode.Keypad0 => "Num0",
                KeyCode.Keypad1 => "Num1",
                KeyCode.Keypad2 => "Num2",
                KeyCode.Keypad3 => "Num3",
                KeyCode.Keypad4 => "Num4",
                KeyCode.Keypad5 => "Num5",
                KeyCode.Keypad6 => "Num6",
                KeyCode.Keypad7 => "Num7",
                KeyCode.Keypad8 => "Num8",
                KeyCode.Keypad9 => "Num9",
                KeyCode.LeftShift => "LShift",
                KeyCode.RightShift => "RShift",
                KeyCode.LeftControl => "LCtrl",
                KeyCode.RightControl => "RCtrl",
                KeyCode.Space => "Space",
                KeyCode.Tab => "Tab",
                KeyCode.Return => "Enter",
                KeyCode.Backspace => "Bksp",
                KeyCode.Escape => "Esc",
                _ => key.ToString().Length > 5 ? key.ToString().Substring(0, 5) : key.ToString()
            };
        }

        public static IEnumerator Show(UIManager ui)
        {
            if (_keybindsMenuScreen == null)
            {
                Plugin.Log.LogError("Keybinds screen not initialized!");
                yield break;
            }

            _isActive = true;
            RefreshAllDisplays();

            // NOTE: We transition from SS Manager, not main menu
            // So we don't need to hide main menu here (it's already hidden)

            var cg = _keybindsMenuScreen.GetComponent<CanvasGroup>();
            _keybindsMenuScreen.gameObject.SetActive(true);

            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;

                float alpha = 0f;
                while (alpha < 1f)
                {
                    alpha += Time.unscaledDeltaTime * 4f;
                    cg.alpha = alpha;
                    yield return null;
                }
                cg.alpha = 1f;
            }

            // Focus first entry
            if (_mappableEntries.Count > 0)
            {
                EventSystem.current?.SetSelectedGameObject(_mappableEntries[0].button.gameObject);
            }
        }

        private static void HideMainMenuElements(UIManager ui)
        {
            // Use same logic as GoToModMenu in MainMenuHook
            MainMenuHook.HideMainMenu(ui);
        }

        private static void ShowMainMenuElements(UIManager ui)
        {
            // Use same logic as ReturnToMainMenu in MainMenuHook
            MainMenuHook.ShowMainMenu(ui);
        }

        public static IEnumerator Hide(UIManager ui)
        {
            if (_keybindsMenuScreen == null) yield break;

            _isActive = false;

            // NOTE: We return to SS Manager, not main menu
            // So we don't show main menu here (will be handled by MainMenuHook)

            var cg = _keybindsMenuScreen.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                float alpha = 1f;
                while (alpha > 0f)
                {
                    alpha -= Time.unscaledDeltaTime * 4f;
                    cg.alpha = alpha;
                    yield return null;
                }
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            _keybindsMenuScreen.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Internal struct to track a keybind entry.
    /// </summary>
    internal class ModMappableKeyEntry
    {
        public ModAction action;
        public MenuButton button;
        public Text keyText;
        public Image keyBg;
        public bool isListening;
    }
}
