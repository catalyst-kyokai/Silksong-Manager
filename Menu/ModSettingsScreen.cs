using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace SilksongManager.Menu
{
    /// <summary>
    /// Settings screen for Silksong Manager with native game UI.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class ModSettingsScreen
    {
        private static MenuScreen _settingsScreen;
        private static bool _initialized = false;
        private static bool _isActive = false;
        
        public static bool IsActive => _isActive;
        
        public static void Initialize()
        {
            if (_initialized) return;
            
            try
            {
                CreateSettingsScreen();
                _initialized = true;
                Plugin.Log.LogInfo("ModSettingsScreen initialized");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to initialize ModSettingsScreen: {e}");
            }
        }
        
        public static void Reset()
        {
            _initialized = false;
            _settingsScreen = null;
            _isActive = false;
        }
        
        private static void CreateSettingsScreen()
        {
            var ui = UIManager.instance;
            if (ui == null)
            {
                Plugin.Log.LogError("UIManager not found!");
                return;
            }
            
            var templateScreen = ui.extrasMenuScreen;
            if (templateScreen == null)
            {
                Plugin.Log.LogError("ExtrasMenuScreen not found!");
                return;
            }
            
            // Clone the screen
            var screenObj = Object.Instantiate(templateScreen.gameObject, templateScreen.transform.parent);
            screenObj.name = "ModSettingsScreen";
            
            _settingsScreen = screenObj.GetComponent<MenuScreen>();
            if (_settingsScreen == null)
            {
                Object.Destroy(screenObj);
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
            
            ModifyScreenContent(screenObj);
        }
        
        private static void ModifyScreenContent(GameObject screenObj)
        {
            // Save back button first
            MenuButton savedBackButton = null;
            if (_settingsScreen.backButton != null)
            {
                savedBackButton = _settingsScreen.backButton;
                savedBackButton.transform.SetParent(screenObj.transform, false);
                savedBackButton.gameObject.SetActive(false);
            }
            
            // Destroy all children except title and fleur
            var toDestroy = new System.Collections.Generic.List<GameObject>();
            Transform titleTransform = null;
            
            foreach (Transform child in screenObj.transform)
            {
                if (savedBackButton != null && child == savedBackButton.transform)
                    continue;
                    
                var name = child.name.ToLower();
                if (name.Contains("title"))
                {
                    titleTransform = child;
                }
                else if (name.Contains("fleur"))
                {
                    // Keep fleur
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
                    textComp.text = "Settings";
            }
            
            // Configure back button
            if (savedBackButton != null)
            {
                savedBackButton.OnSubmitPressed = new UnityEvent();
                savedBackButton.OnSubmitPressed.AddListener(OnBackPressed);
                savedBackButton.gameObject.SetActive(true);
                
                var backRect = savedBackButton.GetComponent<RectTransform>();
                if (backRect != null)
                {
                    backRect.anchorMin = new Vector2(0.5f, 0);
                    backRect.anchorMax = new Vector2(0.5f, 0);
                    backRect.pivot = new Vector2(0.5f, 0.5f);
                    backRect.anchoredPosition = new Vector2(0, 80);
                }
                
                DestroyLocalization(savedBackButton.gameObject);
            }
            
            // Create settings content
            CreateSettingsContent(screenObj, savedBackButton);
        }
        
        private static void CreateSettingsContent(GameObject screenObj, MenuButton backButton)
        {
            // Create settings toggles using cloned buttons
            float startY = 80;
            float spacing = -50;
            int index = 0;
            
            // Pause Game on Menu
            var pauseToggle = CreateToggleButton(screenObj, "PauseGameToggle", 
                "Pause Game on Menu Open", startY + (index++ * spacing),
                () => DebugMenu.DebugMenuConfig.PauseGameOnMenu,
                (val) => DebugMenu.DebugMenuConfig.PauseGameOnMenu = val);
            
            // Enable Hotkeys
            var hotkeysToggle = CreateToggleButton(screenObj, "HotkeysToggle",
                "Enable Hotkeys", startY + (index++ * spacing),
                () => Plugin.ModConfig.EnableHotkeys,
                (val) => Plugin.ModConfig.EnableHotkeys = val);
            
            // Setup navigation
            MenuButton firstButton = pauseToggle?.GetComponent<MenuButton>();
            MenuButton secondButton = hotkeysToggle?.GetComponent<MenuButton>();
            
            if (firstButton != null && secondButton != null && backButton != null)
            {
                SetupNavigation(firstButton, backButton, secondButton);
                SetupNavigation(secondButton, firstButton, backButton);
                
                var backNav = backButton.navigation;
                backNav.mode = Navigation.Mode.Explicit;
                backNav.selectOnUp = secondButton;
                backNav.selectOnDown = firstButton;
                backButton.navigation = backNav;
                
                _settingsScreen.defaultHighlight = firstButton;
            }
        }
        
        private static GameObject CreateToggleButton(GameObject parent, string name, string label, float yOffset,
            Func<bool> getter, Action<bool> setter)
        {
            if (_settingsScreen?.backButton == null) return null;
            
            var buttonObj = Object.Instantiate(_settingsScreen.backButton.gameObject, parent.transform);
            buttonObj.name = name;
            
            var rect = buttonObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0, yOffset);
            }
            
            var menuButton = buttonObj.GetComponent<MenuButton>();
            if (menuButton != null)
            {
                menuButton.OnSubmitPressed = new UnityEvent();
                menuButton.OnSubmitPressed.AddListener(() =>
                {
                    bool newVal = !getter();
                    setter(newVal);
                    UpdateToggleText(buttonObj, label, newVal);
                });
                
                DestroyLocalization(buttonObj);
                UpdateToggleText(buttonObj, label, getter());
            }
            
            return buttonObj;
        }
        
        private static void UpdateToggleText(GameObject buttonObj, string label, bool isOn)
        {
            var text = buttonObj.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = $"{label}: {(isOn ? "ON" : "OFF")}";
            }
            
            var tmp = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.text = $"{label}: {(isOn ? "ON" : "OFF")}";
            }
        }
        
        private static void SetupNavigation(MenuButton button, MenuButton up, MenuButton down)
        {
            if (button == null) return;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = up;
            nav.selectOnDown = down;
            button.navigation = nav;
        }
        
        private static void DestroyLocalization(GameObject obj)
        {
            if (obj == null) return;
            var components = obj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var typeName = comp.GetType().Name;
                if (typeName.Contains("Locali") || typeName.Contains("Translat"))
                {
                    Object.DestroyImmediate(comp);
                }
            }
        }
        
        private static void OnBackPressed()
        {
            Plugin.Log.LogInfo("Settings back button pressed");
            MainMenuHook.ReturnFromSettingsScreen();
        }
        
        public static IEnumerator Show(UIManager ui)
        {
            if (_settingsScreen == null) yield break;
            
            _isActive = true;
            var cg = _settingsScreen.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                _settingsScreen.gameObject.SetActive(true);
                cg.alpha = 0f;
                float fade = 0f;
                while (fade < 1f)
                {
                    fade += Time.unscaledDeltaTime * ui.MENU_FADE_SPEED;
                    cg.alpha = fade;
                    yield return null;
                }
                cg.alpha = 1f;
                cg.interactable = true;
            }
            else
            {
                _settingsScreen.gameObject.SetActive(true);
            }
            
            _settingsScreen.HighlightDefault();
        }
        
        public static IEnumerator Hide(UIManager ui)
        {
            if (_settingsScreen == null) yield break;
            
            _isActive = false;
            var cg = _settingsScreen.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                float fade = 1f;
                while (fade > 0f)
                {
                    fade -= Time.unscaledDeltaTime * ui.MENU_FADE_SPEED;
                    cg.alpha = fade;
                    yield return null;
                }
                cg.alpha = 0f;
                cg.interactable = false;
            }
            _settingsScreen.gameObject.SetActive(false);
        }
    }
}
