using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SilksongManager.Menu
{
    /// <summary>
    /// Custom menu screen for Silksong Manager mod settings.
    /// Uses Unity UI (Canvas) for native look and feel.
    /// </summary>
    public class ModMenuScreen : MonoBehaviour
    {
        private bool _isVisible = false;
        private GameObject _menuCanvas;
        private CanvasGroup _canvasGroup;

        public void Initialize()
        {
            CreateMenuUI();
            Hide();
        }

        private void CreateMenuUI()
        {
            // Create Canvas
            _menuCanvas = new GameObject("SSManagerCanvas");
            _menuCanvas.transform.SetParent(transform);
            
            var canvas = _menuCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // Above game UI
            
            var scaler = _menuCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            _menuCanvas.AddComponent<GraphicRaycaster>();
            _canvasGroup = _menuCanvas.AddComponent<CanvasGroup>();

            // Create background panel
            var bgPanel = CreatePanel(_menuCanvas.transform, "Background", 
                new Vector2(0, 0), new Vector2(1, 1));
            var bgImage = bgPanel.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            // Create title
            CreateText(bgPanel.transform, "Title", "SS Manager", 48, 
                new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f));

            // Create subtitle
            CreateText(bgPanel.transform, "Subtitle", "Mod Settings", 24, 
                new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f));

            // Create buttons container
            var buttonsContainer = CreatePanel(bgPanel.transform, "ButtonsContainer",
                new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f));

            // Create Keybinds button
            CreateMenuButton(buttonsContainer.transform, "KeybindsButton", "Keybinds", 
                new Vector2(0.5f, 0.7f), OnKeybindsPressed);

            // Create Back button
            CreateMenuButton(buttonsContainer.transform, "BackButton", "Back", 
                new Vector2(0.5f, 0.2f), OnBackPressed);

            // Create version text
            CreateText(bgPanel.transform, "Version", $"v{PluginInfo.VERSION}", 16, 
                new Vector2(0.5f, 0.05f), new Vector2(0.5f, 0.05f));
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            return panel;
        }

        private GameObject CreateText(Transform parent, string name, string text, int fontSize, 
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = new Vector2(600, 60);
            rect.anchoredPosition = Vector2.zero;
            
            var textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            if (textComp.font == null)
            {
                textComp.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            }
            
            return textObj;
        }

        private GameObject CreateMenuButton(Transform parent, string name, string text, 
            Vector2 anchor, Action onClick)
        {
            var buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            
            var rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(300, 60);
            rect.anchoredPosition = Vector2.zero;
            
            // Background image
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.3f, 1f);
            
            // Button component
            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            
            // Color transitions
            var colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
            colors.pressedColor = new Color(0.4f, 0.4f, 0.6f, 1f);
            colors.selectedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
            button.colors = colors;
            
            // Click handler
            button.onClick.AddListener(() => onClick?.Invoke());
            
            // Text child
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            textComp.fontSize = 28;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            if (textComp.font == null)
            {
                textComp.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
            }
            
            return buttonObj;
        }

        public void Show()
        {
            _isVisible = true;
            if (_menuCanvas != null)
            {
                _menuCanvas.SetActive(true);
            }
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            // Pause the game if we're not in the main menu scene
            if (GameManager.instance != null && !GameManager.instance.IsMenuScene())
            {
                Time.timeScale = 0f;
            }

            Plugin.Log.LogInfo("SS Manager menu opened.");
        }

        public void Hide()
        {
            _isVisible = false;
            if (_menuCanvas != null)
            {
                _menuCanvas.SetActive(false);
            }
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            // Resume the game if we paused it
            if (GameManager.instance != null && !GameManager.instance.IsMenuScene())
            {
                Time.timeScale = 1f;
            }
        }

        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public bool IsVisible => _isVisible;

        private void OnKeybindsPressed()
        {
            Plugin.Log.LogInfo("Keybinds button pressed - not yet implemented.");
            // TODO: Show keybinds submenu
        }

        private void OnBackPressed()
        {
            Plugin.Log.LogInfo("Back button pressed, returning to main menu.");
            Hide();
            
            // Return to main menu
            var ui = UIManager.instance;
            if (ui != null)
            {
                ui.UIGoToMainMenu();
            }
        }

        private void Update()
        {
            // Handle Escape key to close menu
            if (_isVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                OnBackPressed();
            }
        }

        private void OnDestroy()
        {
            if (_menuCanvas != null)
            {
                Object.Destroy(_menuCanvas);
            }
        }
    }
}
