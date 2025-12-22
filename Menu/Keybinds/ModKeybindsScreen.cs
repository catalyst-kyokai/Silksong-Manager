using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace SilksongManager.Menu.Keybinds
{
    /// <summary>
    /// Builds and manages the mod keybinds menu screen.
    /// </summary>
    public static class ModKeybindsScreen
    {
        private static MenuScreen _keybindsMenuScreen;
        private static KeyConflictDialog _conflictDialog;
        private static List<ModMappableKey> _mappableKeys = new List<ModMappableKey>();
        private static bool _initialized = false;
        
        private static ModMappableKey _pendingRebindKey;
        private static KeyCode _pendingKeyCode;
        
        /// <summary>
        /// Create the keybinds menu screen by cloning the keyboard menu.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            try
            {
                CreateKeybindsScreen();
                _initialized = true;
                Plugin.Log.LogInfo("ModKeybindsScreen initialized");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to initialize ModKeybindsScreen: {e}");
            }
        }
        
        /// <summary>
        /// Reset state when leaving menu scene.
        /// </summary>
        public static void Reset()
        {
            _initialized = false;
            _keybindsMenuScreen = null;
            _conflictDialog = null;
            _mappableKeys.Clear();
            _pendingRebindKey = null;
        }
        
        /// <summary>
        /// Get the keybinds menu screen.
        /// </summary>
        public static MenuScreen GetScreen() => _keybindsMenuScreen;
        
        private static void CreateKeybindsScreen()
        {
            var ui = UIManager.instance;
            if (ui == null)
            {
                Plugin.Log.LogError("UIManager not found!");
                return;
            }
            
            // Clone keyboard menu as template (it has the right layout for keybinds)
            MenuScreen templateScreen = ui.keyboardMenuScreen ?? ui.optionsMenuScreen;
            if (templateScreen == null)
            {
                Plugin.Log.LogError("Could not find template MenuScreen!");
                return;
            }
            
            // Clone
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
            
            // Rebuild content
            RebuildScreenContent(screenObj);
            
            // Create conflict dialog
            _conflictDialog = KeyConflictDialog.Create(screenObj.transform);
            
            Plugin.Log.LogInfo("Keybinds menu screen created");
        }
        
        private static void RebuildScreenContent(GameObject screenObj)
        {
            // First, save the backButton before destroying anything
            MenuButton savedBackButton = null;
            if (_keybindsMenuScreen.backButton != null)
            {
                savedBackButton = _keybindsMenuScreen.backButton;
                savedBackButton.transform.SetParent(screenObj.transform, false);
                savedBackButton.gameObject.SetActive(false);
            }
            
            // Destroy all children except saved ones
            var toDestroy = new List<GameObject>();
            Transform titleTransform = null;
            Transform topFleurTransform = null;
            
            foreach (Transform child in screenObj.transform)
            {
                if (savedBackButton != null && child == savedBackButton.transform) continue;
                
                var childName = child.name.ToLower();
                if (childName.Contains("title"))
                {
                    titleTransform = child;
                }
                else if (childName.Contains("fleur"))
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
            
            // Configure title
            if (titleTransform != null)
            {
                titleTransform.gameObject.SetActive(true);
                var titleText = titleTransform.GetComponent<Text>();
                if (titleText != null)
                {
                    // Destroy localization
                    foreach (var comp in titleTransform.GetComponents<MonoBehaviour>())
                    {
                        if (comp.GetType().Name.Contains("Locali"))
                        {
                            Object.DestroyImmediate(comp);
                        }
                    }
                    titleText.text = "Mod Keybinds";
                }
            }
            
            // Enable fleur
            if (topFleurTransform != null)
            {
                topFleurTransform.gameObject.SetActive(true);
            }
            
            // Create content container
            var contentObj = new GameObject("ModKeybindsContent");
            contentObj.transform.SetParent(screenObj.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.1f, 0.2f);
            contentRect.anchorMax = new Vector2(0.9f, 0.85f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            // Add vertical layout
            var layout = contentObj.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            
            // Create keybind entries
            _mappableKeys.Clear();
            foreach (ModAction action in Enum.GetValues(typeof(ModAction)))
            {
                CreateKeybindEntry(contentObj.transform, action);
            }
            
            // Setup back button
            if (savedBackButton != null)
            {
                savedBackButton.OnSubmitPressed = new UnityEvent();
                savedBackButton.OnSubmitPressed.AddListener(OnBackButtonPressed);
                savedBackButton.gameObject.SetActive(true);
                
                // Position at bottom
                var backRect = savedBackButton.GetComponent<RectTransform>();
                if (backRect != null)
                {
                    backRect.anchorMin = new Vector2(0.5f, 0);
                    backRect.anchorMax = new Vector2(0.5f, 0);
                    backRect.pivot = new Vector2(0.5f, 0.5f);
                    backRect.anchoredPosition = new Vector2(0, 60);
                }
                
                // Destroy localization
                foreach (var comp in savedBackButton.GetComponents<MonoBehaviour>())
                {
                    if (comp.GetType().Name.Contains("Locali"))
                    {
                        Object.DestroyImmediate(comp);
                    }
                }
            }
            
            // Set navigation
            if (_mappableKeys.Count > 0)
            {
                _keybindsMenuScreen.defaultHighlight = _mappableKeys[0];
                
                // Wire up navigation
                for (int i = 0; i < _mappableKeys.Count; i++)
                {
                    var nav = _mappableKeys[i].navigation;
                    nav.mode = Navigation.Mode.Explicit;
                    nav.selectOnUp = i > 0 ? _mappableKeys[i - 1] : savedBackButton;
                    nav.selectOnDown = i < _mappableKeys.Count - 1 ? _mappableKeys[i + 1] : savedBackButton;
                    _mappableKeys[i].navigation = nav;
                }
                
                if (savedBackButton != null)
                {
                    var backNav = savedBackButton.navigation;
                    backNav.mode = Navigation.Mode.Explicit;
                    backNav.selectOnUp = _mappableKeys[_mappableKeys.Count - 1];
                    backNav.selectOnDown = _mappableKeys[0];
                    savedBackButton.navigation = backNav;
                }
            }
        }
        
        private static void CreateKeybindEntry(Transform parent, ModAction action)
        {
            var entryObj = new GameObject($"Keybind_{action}");
            entryObj.transform.SetParent(parent, false);
            
            var entryRect = entryObj.AddComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(0, 45);
            
            // Horizontal layout
            var hLayout = entryObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.spacing = 20;
            hLayout.padding = new RectOffset(20, 20, 5, 5);
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            
            // Action label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(entryObj.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(250, 40);
            
            var labelText = labelObj.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 24;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            
            // Key display button
            var keyBtnObj = new GameObject("KeyButton");
            keyBtnObj.transform.SetParent(entryObj.transform, false);
            var keyRect = keyBtnObj.AddComponent<RectTransform>();
            keyRect.sizeDelta = new Vector2(120, 40);
            
            var keyBg = keyBtnObj.AddComponent<Image>();
            keyBg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
            
            // Key text
            var keyTextObj = new GameObject("KeyText");
            keyTextObj.transform.SetParent(keyBtnObj.transform, false);
            var keyTextRect = keyTextObj.AddComponent<RectTransform>();
            keyTextRect.anchorMin = Vector2.zero;
            keyTextRect.anchorMax = Vector2.one;
            keyTextRect.offsetMin = new Vector2(5, 0);
            keyTextRect.offsetMax = new Vector2(-5, 0);
            
            var keyText = keyTextObj.AddComponent<Text>();
            keyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            keyText.fontSize = 22;
            keyText.alignment = TextAnchor.MiddleCenter;
            keyText.color = new Color(0.9f, 0.85f, 0.7f);
            
            // Add ModMappableKey component
            var mappableKey = keyBtnObj.AddComponent<ModMappableKey>();
            mappableKey.Initialize(action, labelText, keyText, keyBg, OnKeySelected);
            
            _mappableKeys.Add(mappableKey);
        }
        
        private static void OnKeySelected(ModMappableKey mappableKey, KeyCode key)
        {
            // Check for conflicts with other mod keybinds
            if (ModKeybindManager.IsModKeybindConflicting(key, mappableKey.Action, out ModAction conflictAction))
            {
                // Show conflict dialog
                _pendingRebindKey = mappableKey;
                _pendingKeyCode = key;
                
                string conflictName = ModKeybindManager.GetActionName(conflictAction);
                _conflictDialog.Show(key, conflictName, 
                    () => OnConflictCombine(conflictAction),
                    () => OnConflictReplace(conflictAction),
                    OnConflictCancel);
                return;
            }
            
            // Check for game keybind conflicts
            if (ModKeybindManager.IsGameKeybindConflicting(key, out string gameAction))
            {
                _pendingRebindKey = mappableKey;
                _pendingKeyCode = key;
                
                _conflictDialog.Show(key, $"Game: {gameAction}", 
                    () => OnConflictCombine(default), // Combine with game action (just apply)
                    () => OnConflictCombine(default), // Replace doesn't make sense for game binds
                    OnConflictCancel);
                return;
            }
            
            // No conflict, apply directly
            mappableKey.ApplyKeybind(key);
        }
        
        private static void OnConflictCombine(ModAction conflictAction)
        {
            // Just apply the new keybind (both actions will fire on same key)
            if (_pendingRebindKey != null)
            {
                _pendingRebindKey.ApplyKeybind(_pendingKeyCode);
                _pendingRebindKey = null;
            }
        }
        
        private static void OnConflictReplace(ModAction conflictAction)
        {
            // Remove keybind from conflicting action, apply to new action
            ModKeybindManager.SetKeybind(conflictAction, KeyCode.None);
            
            if (_pendingRebindKey != null)
            {
                _pendingRebindKey.ApplyKeybind(_pendingKeyCode);
                _pendingRebindKey = null;
            }
            
            // Refresh all displays
            foreach (var mk in _mappableKeys)
            {
                mk.ShowCurrentBinding();
            }
        }
        
        private static void OnConflictCancel()
        {
            // Just cancel, restore original
            if (_pendingRebindKey != null)
            {
                _pendingRebindKey.ShowCurrentBinding();
                _pendingRebindKey = null;
            }
        }
        
        private static void OnBackButtonPressed()
        {
            Plugin.Log.LogInfo("Back button pressed from Keybinds screen");
            MainMenuHook.ReturnFromKeybindsScreen();
        }
        
        /// <summary>
        /// Show the keybinds screen.
        /// </summary>
        public static IEnumerator Show(UIManager ui)
        {
            if (_keybindsMenuScreen == null)
            {
                Plugin.Log.LogError("Keybinds screen not initialized!");
                yield break;
            }
            
            // Refresh all keybind displays
            foreach (var mk in _mappableKeys)
            {
                mk.ShowCurrentBinding();
            }
            
            var cg = _keybindsMenuScreen.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                _keybindsMenuScreen.gameObject.SetActive(true);
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
            else
            {
                _keybindsMenuScreen.gameObject.SetActive(true);
            }
            
            // Focus first element
            if (_mappableKeys.Count > 0)
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(_mappableKeys[0].gameObject);
            }
        }
        
        /// <summary>
        /// Hide the keybinds screen.
        /// </summary>
        public static IEnumerator Hide(UIManager ui)
        {
            if (_keybindsMenuScreen == null) yield break;
            
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
}
