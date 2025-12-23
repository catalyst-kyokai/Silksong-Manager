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
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class ModKeybindsScreen
    {
        #region Private Fields

        /// <summary>The keybinds menu screen.</summary>
        private static MenuScreen _keybindsMenuScreen;
        /// <summary>List of mappable key entries.</summary>
        private static List<ModMappableKeyEntry> _mappableEntries = new List<ModMappableKeyEntry>();
        /// <summary>Whether the screen has been initialized.</summary>
        private static bool _initialized = false;
        /// <summary>Whether the screen is currently active.</summary>
        private static bool _isActive = false;

        /// <summary>Pending entry for conflict resolution.</summary>
        private static ModMappableKeyEntry _pendingEntry;
        /// <summary>Pending keycode for conflict resolution.</summary>
        private static KeyCode _pendingKeyCode;

        #endregion

        #region Initialization

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

        /// <summary>
        /// Resets the screen state.
        /// </summary>
        public static void Reset()
        {
            _initialized = false;
            _keybindsMenuScreen = null;
            _mappableEntries.Clear();
            _isActive = false;
        }

        /// <summary>Gets the keybinds menu screen.</summary>
        public static MenuScreen GetScreen() => _keybindsMenuScreen;
        /// <summary>Whether the screen is currently active.</summary>
        public static bool IsActive => _isActive;

        #endregion

        #region Screen Creation

        private static void CreateFromKeyboardMenu()
        {
            var ui = UIManager.instance;
            if (ui == null)
            {
                Plugin.Log.LogError("UIManager not found!");
                return;
            }

            // Use ExtrasMenuScreen as template - same as SS Manager does
            // This avoids pulling in optionsMenuScreen logic
            var templateScreen = ui.extrasMenuScreen;
            if (templateScreen == null)
            {
                Plugin.Log.LogError("ExtrasMenuScreen not found!");
                return;
            }

            Plugin.Log.LogInfo($"Creating ModKeybindsScreen from {templateScreen.name}");

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

            // Remove MenuButtonList which may have original navigation logic
            var menuButtonList = screenObj.GetComponent<MenuButtonList>();
            if (menuButtonList != null)
            {
                Object.DestroyImmediate(menuButtonList);
                Plugin.Log.LogInfo("Removed MenuButtonList from cloned screen");
            }

            // Hide initially
            screenObj.SetActive(false);
            var cg = screenObj.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
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
                    textComp.text = "Silksong Manager Keybinds";
                    _gameFont = textComp.font;
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

            // Create Reset Button (above back button)
            if (savedBackButton != null)
            {
                var resetBtn = Object.Instantiate(savedBackButton.gameObject, savedBackButton.transform.parent);
                resetBtn.name = "ResetButton";
                resetBtn.SetActive(true);

                // Remove existing click listeners
                var menuBtn = resetBtn.GetComponent<MenuButton>();
                if (menuBtn != null)
                {
                    menuBtn.OnSubmitPressed = new UnityEvent();
                    menuBtn.OnSubmitPressed.AddListener(() =>
                    {
                        ModKeybindManager.ResetToDefaults();
                        RefreshAllDisplays();
                    });
                }

                // Change text
                var txt = resetBtn.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = "RESET TO DEFAULTS";
                }

                // Position
                var rect = resetBtn.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0);
                    rect.anchorMax = new Vector2(0.5f, 0);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0, 120); // Above back button
                }

                _resetButton = menuBtn;
            }

            // Set default highlight
            if (_mappableEntries.Count > 0)
            {
                _keybindsMenuScreen.defaultHighlight = _mappableEntries[0].button;
            }
        }

        private static MenuButton _resetButton;
        private static ScrollRect _scrollRect;
        private static Scrollbar _scrollbar;

        private static GameObject CreateContentContainer(Transform parent, MappableKey templateKey)
        {
            // Create scroll area container
            var scrollAreaObj = new GameObject("ScrollArea");
            scrollAreaObj.transform.SetParent(parent, false);

            var scrollAreaRect = scrollAreaObj.AddComponent<RectTransform>();
            // Position: leave space for title at top and buttons at bottom
            scrollAreaRect.anchorMin = new Vector2(0.05f, 0.22f);  // Higher bottom margin for buttons
            scrollAreaRect.anchorMax = new Vector2(0.95f, 0.82f);  // Leave room for title
            scrollAreaRect.offsetMin = Vector2.zero;
            scrollAreaRect.offsetMax = Vector2.zero;

            // Create viewport with mask
            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollAreaObj.transform, false);

            var viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = new Vector2(0.96f, 1f);  // Leave space for scrollbar on right
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            viewportObj.AddComponent<RectMask2D>();
            var viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = Color.clear;

            // Create content panel inside viewport
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            // Content size will be set by ContentSizeFitter
            var sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Add vertical layout to content
            var contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.spacing = 10;
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.padding = new RectOffset(0, 0, 10, 10);

            // Create two-column row containers
            var actions = (ModAction[])Enum.GetValues(typeof(ModAction));
            int halfCount = (actions.Length + 1) / 2;

            _mappableEntries.Clear();

            // Create entries in rows with proper two-column layout
            // Key insight: Row needs explicit width for child anchors to work
            for (int i = 0; i < halfCount; i++)
            {
                var rowObj = new GameObject($"Row_{i}");
                rowObj.transform.SetParent(contentObj.transform, false);

                var rowRect = rowObj.AddComponent<RectTransform>();
                // Let VerticalLayoutGroup control size
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);

                // Add layout element to control row height
                var rowLayoutElement = rowObj.AddComponent<LayoutElement>();
                rowLayoutElement.preferredHeight = 60;
                rowLayoutElement.minHeight = 60;

                // === LEFT HALF (0% - 40% of row width) ===
                var leftHalfObj = new GameObject("LeftHalf");
                leftHalfObj.transform.SetParent(rowObj.transform, false);
                var leftHalfRect = leftHalfObj.AddComponent<RectTransform>();
                leftHalfRect.anchorMin = new Vector2(0f, 0f);
                leftHalfRect.anchorMax = new Vector2(0.40f, 1f);  // 40% width
                leftHalfRect.offsetMin = Vector2.zero;
                leftHalfRect.offsetMax = Vector2.zero;
                leftHalfRect.pivot = new Vector2(1f, 0.5f);  // Right pivot for right alignment

                // Left entry (right-aligned within left half)
                var leftEntry = CreateKeybindEntry(leftHalfObj.transform, actions[i], templateKey, TextAnchor.MiddleRight);
                _mappableEntries.Add(leftEntry);

                // === RIGHT HALF (60% - 100% of row width) ===
                // 20% gap between 40% and 60%
                int rightIndex = i + halfCount;
                if (rightIndex < actions.Length)
                {
                    var rightHalfObj = new GameObject("RightHalf");
                    rightHalfObj.transform.SetParent(rowObj.transform, false);
                    var rightHalfRect = rightHalfObj.AddComponent<RectTransform>();
                    rightHalfRect.anchorMin = new Vector2(0.60f, 0f);  // Start at 60%
                    rightHalfRect.anchorMax = new Vector2(1f, 1f);
                    rightHalfRect.offsetMin = Vector2.zero;
                    rightHalfRect.offsetMax = Vector2.zero;
                    rightHalfRect.pivot = new Vector2(0f, 0.5f);  // Left pivot for left alignment

                    // Right entry (left-aligned within right half)
                    var rightEntry = CreateKeybindEntry(rightHalfObj.transform, actions[rightIndex], templateKey, TextAnchor.MiddleLeft);
                    _mappableEntries.Add(rightEntry);
                }

                // Debug log first row
                if (i == 0)
                {
                    Plugin.Log.LogInfo($"[KeybindsLayout] Row_0 created. LeftHalf anchors: min(0,0) max(0.45,1), RightHalf anchors: min(0.55,0) max(1,1)");
                }
            }

            Plugin.Log.LogInfo($"[KeybindsLayout] Created {halfCount} rows with {_mappableEntries.Count} entries");

            // Clone scrollbar from achievements screen
            _scrollbar = CloneAchievementsScrollbar(scrollAreaObj.transform);

            // Add ScrollRect component
            _scrollRect = scrollAreaObj.AddComponent<ScrollRect>();
            _scrollRect.content = contentRect;
            _scrollRect.viewport = viewportRect;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 30f;
            _scrollRect.inertia = true;
            _scrollRect.decelerationRate = 0.135f;

            if (_scrollbar != null)
            {
                _scrollRect.verticalScrollbar = _scrollbar;
                _scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                _scrollRect.verticalScrollbarSpacing = -5f;
            }

            // Setup navigation between entries
            SetupNavigation();

            return scrollAreaObj;
        }

        private static Scrollbar CloneAchievementsScrollbar(Transform parent)
        {
            try
            {
                var achievementsScreen = UIManager.instance.achievementsMenuScreen;
                if (achievementsScreen == null)
                {
                    Plugin.Log.LogWarning("Achievements screen not found, creating default scrollbar");
                    return CreateDefaultScrollbar(parent);
                }

                var templateScrollbar = achievementsScreen.GetComponentInChildren<Scrollbar>(true);
                if (templateScrollbar == null)
                {
                    Plugin.Log.LogWarning("Scrollbar not found in achievements screen, creating default");
                    return CreateDefaultScrollbar(parent);
                }

                // Clone the scrollbar
                var scrollbarObj = Object.Instantiate(templateScrollbar.gameObject, parent);
                scrollbarObj.name = "Scrollbar";
                scrollbarObj.SetActive(true);

                var scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
                // Position on the right side
                scrollbarRect.anchorMin = new Vector2(0.97f, 0.05f);
                scrollbarRect.anchorMax = new Vector2(1f, 0.95f);
                scrollbarRect.offsetMin = Vector2.zero;
                scrollbarRect.offsetMax = Vector2.zero;

                var scrollbar = scrollbarObj.GetComponent<Scrollbar>();
                scrollbar.direction = Scrollbar.Direction.BottomToTop;

                Plugin.Log.LogInfo("Cloned scrollbar from achievements screen");
                return scrollbar;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to clone scrollbar: {e.Message}");
                return CreateDefaultScrollbar(parent);
            }
        }

        private static Scrollbar CreateDefaultScrollbar(Transform parent)
        {
            // Create a simple scrollbar if cloning fails
            var scrollbarObj = new GameObject("Scrollbar");
            scrollbarObj.transform.SetParent(parent, false);

            var scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(0.97f, 0.05f);
            scrollbarRect.anchorMax = new Vector2(1f, 0.95f);
            scrollbarRect.offsetMin = Vector2.zero;
            scrollbarRect.offsetMax = Vector2.zero;

            // Background
            var bgImage = scrollbarObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            // Handle
            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(scrollbarObj.transform, false);

            var handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = new Vector2(2, 2);
            handleRect.offsetMax = new Vector2(-2, -2);

            var handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);

            var scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            return scrollbar;
        }


        private static ModMappableKeyEntry CreateKeybindEntry(Transform parent, ModAction action, MappableKey templateKey, TextAnchor alignment)
        {
            var entryObj = new GameObject($"Entry_{action}");
            entryObj.transform.SetParent(parent, false);

            var entryRect = entryObj.AddComponent<RectTransform>();
            // Stretch to fill parent container (LeftHalf or RightHalf)
            entryRect.anchorMin = Vector2.zero;
            entryRect.anchorMax = Vector2.one;
            entryRect.offsetMin = Vector2.zero;
            entryRect.offsetMax = Vector2.zero;

            // Horizontal layout like game: label [key]
            var hLayout = entryObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = alignment;  // This aligns children within Entry
            hLayout.spacing = 15;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.padding = new RectOffset(5, 5, 0, 0);

            // Label (left side) - wider to fit text
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(entryObj.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(250, 45);
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 250;
            labelLayout.flexibleWidth = 0;

            var labelText = labelObj.AddComponent<Text>();
            labelText.font = GetGameFont();
            labelText.fontSize = 42; // Larger font
            labelText.fontStyle = FontStyle.Normal;
            labelText.alignment = TextAnchor.MiddleRight;
            labelText.color = Color.white;
            labelText.text = ModKeybindManager.GetActionName(action).ToUpper();
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;  // Don't truncate

            // Key button (right side) - styled like game
            var keyBtnObj = new GameObject("KeyButton");
            keyBtnObj.transform.SetParent(entryObj.transform, false);
            var keyRect = keyBtnObj.AddComponent<RectTransform>();
            keyRect.sizeDelta = new Vector2(90, 55);
            var keyLayout = keyBtnObj.AddComponent<LayoutElement>();
            keyLayout.preferredWidth = 90;
            keyLayout.preferredHeight = 55;
            keyLayout.flexibleWidth = 0;

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
            keyText.fontSize = 34; // Larger key font
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
            // Entries are added in pairs: [0]=left1, [1]=right1, [2]=left2, [3]=right2, etc.
            // Navigation: Up/Down moves between rows, Left/Right moves within row

            int numEntries = _mappableEntries.Count;
            int numRows = (numEntries + 1) / 2;

            for (int i = 0; i < numEntries; i++)
            {
                var entry = _mappableEntries[i];
                var nav = entry.button.navigation;
                nav.mode = Navigation.Mode.Explicit;

                bool isLeft = (i % 2) == 0;
                int row = i / 2;

                // Horizontal navigation within row
                if (isLeft)
                {
                    // Left entry: right goes to right entry in same row
                    int rightIndex = i + 1;
                    if (rightIndex < numEntries)
                        nav.selectOnRight = _mappableEntries[rightIndex].button;
                }
                else
                {
                    // Right entry: left goes to left entry in same row
                    nav.selectOnLeft = _mappableEntries[i - 1].button;
                }

                // Vertical navigation between rows
                // Up: go to same side in previous row
                if (row > 0)
                {
                    int prevRowSameSide = (row - 1) * 2 + (isLeft ? 0 : 1);
                    if (prevRowSameSide < numEntries)
                        nav.selectOnUp = _mappableEntries[prevRowSameSide].button;
                }
                else
                {
                    // First row: up goes to back button or first entry
                    if (_keybindsMenuScreen.backButton != null)
                        nav.selectOnUp = _keybindsMenuScreen.backButton;
                }

                // Down: go to same side in next row
                if (row < numRows - 1)
                {
                    int nextRowSameSide = (row + 1) * 2 + (isLeft ? 0 : 1);
                    if (nextRowSameSide < numEntries)
                        nav.selectOnDown = _mappableEntries[nextRowSameSide].button;
                    else if ((row + 1) * 2 < numEntries)
                        nav.selectOnDown = _mappableEntries[(row + 1) * 2].button; // Go to left if right doesn't exist
                }
                else
                {
                    // Last row: down goes to Reset button
                    if (_resetButton != null)
                        nav.selectOnDown = _resetButton;
                }

                entry.button.navigation = nav;
            }

            // Setup Reset Button navigation
            if (_resetButton != null)
            {
                var nav = _resetButton.navigation;
                nav.mode = Navigation.Mode.Explicit;

                // Up: last row's left entry
                if (numEntries > 0)
                {
                    int lastRowLeft = ((numEntries - 1) / 2) * 2;
                    nav.selectOnUp = _mappableEntries[lastRowLeft].button;
                }

                // Down: Back button
                if (_keybindsMenuScreen.backButton != null)
                    nav.selectOnDown = _keybindsMenuScreen.backButton;

                _resetButton.navigation = nav;
            }

            // Fix Back Button navigation
            if (_keybindsMenuScreen.backButton != null)
            {
                var nav = _keybindsMenuScreen.backButton.navigation;
                if (_resetButton != null)
                    nav.selectOnUp = _resetButton;
                else if (numEntries > 0)
                    nav.selectOnUp = _mappableEntries[0].button;

                // Down should go to first entry
                if (numEntries > 0)
                    nav.selectOnDown = _mappableEntries[0].button;

                _keybindsMenuScreen.backButton.navigation = nav;
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

        private static Font _gameFont;

        private static Font GetGameFont()
        {
            if (_gameFont != null) return _gameFont;

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
            // However, ensuring main menu is hidden prevents it from reappearing if UIManager acts up
            MainMenuHook.HideMainMenu(ui);

            var cg = _keybindsMenuScreen.GetComponent<CanvasGroup>();
            _keybindsMenuScreen.gameObject.SetActive(true);

            // Add/enable input controller for Escape handling
            var inputController = _keybindsMenuScreen.gameObject.GetComponent<KeybindsInputController>();
            if (inputController == null)
            {
                inputController = _keybindsMenuScreen.gameObject.AddComponent<KeybindsInputController>();
            }
            inputController.enabled = true;

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

            Plugin.Log.LogInfo("Keybinds screen shown");
        }

        public static bool IsExiting => _isExiting;
        private static bool _isExiting = false;

        public static IEnumerator Hide(UIManager ui)
        {
            if (_keybindsMenuScreen == null) yield break;

            _isExiting = true;

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
            _isActive = false;
            _isExiting = false;
        }

        #endregion
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

    /// <summary>
    /// Controller attached to keybinds screen to handle Escape key.
    /// </summary>
    public class KeybindsInputController : MonoBehaviour
    {
        void Update()
        {
            if (!ModKeybindsScreen.IsActive) return;

            // Failsafe: Ensure main menu stays hidden
            // If the game triggers main menu visibility (e.g. via state reset), we squash it immediately
            var ui = UIManager.instance;
            if (ui != null && ui.mainMenuScreen != null && ui.mainMenuScreen.gameObject.activeSelf)
            {
                ui.mainMenuScreen.gameObject.SetActive(false);

                // Also hide title/subtitle if they appeared
                // Also hide title/subtitle if they appeared
                if (ui.gameTitle != null && (ui.gameTitle.color.a > 0.1f || ui.gameTitle.enabled))
                {
                    ui.gameTitle.color = new Color(ui.gameTitle.color.r, ui.gameTitle.color.g, ui.gameTitle.color.b, 0f);
                    ui.gameTitle.enabled = false;
                }
            }

            // Check for Escape key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (ModKeybindsScreen.IsExiting) return;

                Plugin.Log.LogInfo("Escape pressed in keybinds menu, returning to SS Manager");
                MainMenuHook.ReturnFromKeybindsScreen();
            }
        }
    }
}
