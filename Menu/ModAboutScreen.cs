using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace SilksongManager.Menu
{
    /// <summary>
    /// About screen for Silksong Manager with credits and description.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class ModAboutScreen
    {
        private static MenuScreen _aboutScreen;
        private static bool _initialized = false;
        private static bool _isActive = false;
        private static bool _isExiting = false;

        public static bool IsActive => _isActive;
        public static bool IsExiting => _isExiting;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                CreateAboutScreen();
                _initialized = true;
                Plugin.Log.LogInfo("ModAboutScreen initialized");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to initialize ModAboutScreen: {e}");
            }
        }

        public static void Reset()
        {
            _initialized = false;
            _aboutScreen = null;
            _isActive = false;
        }

        private static void CreateAboutScreen()
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
            screenObj.name = "ModAboutScreen";

            _aboutScreen = screenObj.GetComponent<MenuScreen>();
            if (_aboutScreen == null)
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
                cg.blocksRaycasts = false;
            }

            ModifyScreenContent(screenObj);
        }

        private static void ModifyScreenContent(GameObject screenObj)
        {
            // Save back button first
            MenuButton savedBackButton = null;
            if (_aboutScreen.backButton != null)
            {
                savedBackButton = _aboutScreen.backButton;
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
                    textComp.text = "About";
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
                _aboutScreen.defaultHighlight = savedBackButton;
            }

            // Create about content
            CreateAboutContent(screenObj);
        }

        private static void CreateAboutContent(GameObject screenObj)
        {
            // Find or create a text template
            var canvas = screenObj.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Create description text
            var descObj = new GameObject("DescriptionText");
            descObj.transform.SetParent(screenObj.transform, false);

            var descText = descObj.AddComponent<Text>();
            descText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            descText.fontSize = 24;
            descText.alignment = TextAnchor.MiddleCenter;
            descText.color = Color.white;
            descText.supportRichText = true;
            descText.text = @"<size=32><b>Silksong Manager</b></size>
<size=18>Version " + PluginInfo.VERSION + @"</size>

A comprehensive mod manager and debug toolkit
for Hollow Knight: Silksong

<color=#ffcc00>Features:</color>
• Debug Menu with player/enemy/world controls
• Custom Keybinds System
• Infinite Jumps, Noclip, Invincibility
• Custom Damage System
• And much more!

<size=20><color=#ff6060>❤</color> Created by <color=#88ccff>Catalyst</color></size>
<size=16>catalyst@kyokai.ru | Telegram: @Catalyst_Kyokai</size>";

            var descRect = descObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.5f, 0.5f);
            descRect.anchorMax = new Vector2(0.5f, 0.5f);
            descRect.pivot = new Vector2(0.5f, 0.5f);
            descRect.anchoredPosition = new Vector2(0, 30);
            descRect.sizeDelta = new Vector2(600, 400);

            // Try to use a better font if available
            var existingText = screenObj.GetComponentInChildren<Text>(true);
            if (existingText != null && existingText.font != null)
            {
                descText.font = existingText.font;
            }
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
            if (_isExiting) return;
            Plugin.Log.LogInfo("About back button pressed");
            MainMenuHook.ReturnFromAboutScreen();
        }

        public static IEnumerator Show(UIManager ui)
        {
            if (_aboutScreen == null) yield break;

            _isActive = true;
            _isExiting = false;

            // IMPORTANT: Hide main menu like Keybinds does
            MainMenuHook.HideMainMenu(ui);

            var cg = _aboutScreen.GetComponent<CanvasGroup>();
            _aboutScreen.gameObject.SetActive(true);

            // Add/enable input controller that keeps main menu hidden
            var inputController = _aboutScreen.gameObject.GetComponent<AboutInputController>();
            if (inputController == null)
            {
                inputController = _aboutScreen.gameObject.AddComponent<AboutInputController>();
            }
            inputController.enabled = true;

            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
                cg.alpha = 0f;

                float fade = 0f;
                while (fade < 1f)
                {
                    fade += Time.unscaledDeltaTime * 4f;
                    cg.alpha = fade;
                    yield return null;
                }
                cg.alpha = 1f;
            }

            _aboutScreen.HighlightDefault();
            Plugin.Log.LogInfo("About screen shown");
        }

        public static IEnumerator Hide(UIManager ui)
        {
            if (_aboutScreen == null) yield break;

            _isExiting = true;

            var cg = _aboutScreen.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                float fade = 1f;
                while (fade > 0f)
                {
                    fade -= Time.unscaledDeltaTime * 4f;
                    cg.alpha = fade;
                    yield return null;
                }
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            _aboutScreen.gameObject.SetActive(false);
            _isActive = false;
            _isExiting = false;
        }
    }

    /// <summary>
    /// Controller that handles Escape and keeps main menu hidden.
    /// </summary>
    public class AboutInputController : MonoBehaviour
    {
        void Update()
        {
            if (!ModAboutScreen.IsActive) return;

            // Failsafe: Keep main menu hidden
            var ui = UIManager.instance;
            if (ui != null && ui.mainMenuScreen != null && ui.mainMenuScreen.gameObject.activeSelf)
            {
                ui.mainMenuScreen.gameObject.SetActive(false);

                if (ui.gameTitle != null && (ui.gameTitle.color.a > 0.1f || ui.gameTitle.enabled))
                {
                    ui.gameTitle.color = new Color(ui.gameTitle.color.r, ui.gameTitle.color.g, ui.gameTitle.color.b, 0f);
                    ui.gameTitle.enabled = false;
                }
            }

            // Handle Escape key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (ModAboutScreen.IsExiting) return;
                Plugin.Log.LogInfo("Escape pressed in About, returning to SS Manager");
                MainMenuHook.ReturnFromAboutScreen();
            }
        }
    }
}
