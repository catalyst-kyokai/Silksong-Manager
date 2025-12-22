using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using Object = UnityEngine.Object;

namespace SilksongManager.Menu.Core
{
    /// <summary>
    /// Manages UI templates cloned from game's menu screens for consistent styling.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class MenuTemplates
    {
        private static bool _initialized = false;
        private static GameObject _uiCanvas;
        private static GameObject _menuScreenTemplate;
        private static GameObject _textButtonTemplate;
        private static GameObject _contentPaneTemplate;

        public static bool IsInitialized => _initialized;
        public static GameObject UICanvas => _uiCanvas;

        /// <summary>
        /// Initialize templates from UIManager. Call once when UIManager.Awake completes.
        /// </summary>
        public static void Initialize(UIManager uiManager)
        {
            if (_initialized) return;

            try
            {
                var uiManagerGO = uiManager.gameObject;

                // Find UICanvas
                _uiCanvas = FindChild(uiManagerGO, "UICanvas");
                if (_uiCanvas == null)
                {
                    Plugin.Log.LogError("MenuTemplates: Could not find UICanvas");
                    return;
                }

                // Find OptionsMenuScreen as template
                var optionsScreen = FindChild(_uiCanvas, "OptionsMenuScreen");
                if (optionsScreen == null)
                {
                    Plugin.Log.LogError("MenuTemplates: Could not find OptionsMenuScreen");
                    return;
                }

                // Clone menu screen template
                _menuScreenTemplate = Object.Instantiate(optionsScreen);
                _menuScreenTemplate.SetActive(false);
                _menuScreenTemplate.name = "SSManagerMenuTemplate";
                Object.DontDestroyOnLoad(_menuScreenTemplate);

                // Clean up menu screen template
                CleanupMenuScreenTemplate(_menuScreenTemplate);

                // Clone text button template from options screen
                var sampleButton = FindChild(optionsScreen, "Content/GameOptions");
                if (sampleButton != null)
                {
                    _textButtonTemplate = Object.Instantiate(sampleButton);
                    _textButtonTemplate.SetActive(false);
                    _textButtonTemplate.name = "TextButtonTemplate";
                    Object.DontDestroyOnLoad(_textButtonTemplate);

                    // Rename child button
                    var buttonChild = FindChild(_textButtonTemplate, "GameOptionsButton");
                    if (buttonChild != null)
                    {
                        buttonChild.name = "TextButton";
                        DestroyLocalization(buttonChild);
                    }
                }

                // Get content pane template
                var contentPane = FindChild(_menuScreenTemplate, "Content");
                if (contentPane != null)
                {
                    _contentPaneTemplate = contentPane;
                    // Clear children
                    DestroyAllChildren(contentPane);

                    // Remove layout components that might interfere
                    var vlg = contentPane.GetComponent<VerticalLayoutGroup>();
                    if (vlg != null) Object.Destroy(vlg);

                    var mbl = contentPane.GetComponent<MenuButtonList>();
                    if (mbl != null) Object.Destroy(mbl);
                }

                // Subscribe to UIManager destruction
                uiManagerGO.AddComponent<OnDestroyCallback>().OnDestroyed += () =>
                {
                    _initialized = false;
                    if (_menuScreenTemplate != null) Object.Destroy(_menuScreenTemplate);
                    if (_textButtonTemplate != null) Object.Destroy(_textButtonTemplate);
                    _menuScreenTemplate = null;
                    _textButtonTemplate = null;
                    _uiCanvas = null;
                };

                _initialized = true;
                Plugin.Log.LogInfo("MenuTemplates initialized successfully");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"MenuTemplates.Initialize failed: {e.Message}");
            }
        }

        /// <summary>
        /// Create a new custom menu screen with the given title.
        /// </summary>
        public static GameObject CreateMenuScreen(string title)
        {
            if (!_initialized || _menuScreenTemplate == null)
            {
                Plugin.Log.LogError("MenuTemplates not initialized");
                return null;
            }

            var screen = Object.Instantiate(_menuScreenTemplate);
            screen.name = $"SSManager_{title}Screen";
            screen.transform.SetParent(_uiCanvas.transform, false);
            screen.transform.localPosition = new Vector3(0f, 10f, 0f);

            // Set title
            var titleObj = FindChild(screen, "Title");
            if (titleObj != null)
            {
                var titleText = titleObj.GetComponent<Text>();
                if (titleText != null)
                {
                    titleText.text = title;
                }
            }

            return screen;
        }

        /// <summary>
        /// Create a text button with the given label.
        /// </summary>
        public static GameObject CreateTextButton(string text, Action onClick)
        {
            if (!_initialized || _textButtonTemplate == null)
            {
                Plugin.Log.LogError("MenuTemplates not initialized");
                return null;
            }

            var buttonContainer = Object.Instantiate(_textButtonTemplate);
            buttonContainer.name = $"Button_{text}";
            buttonContainer.SetActive(true);

            var buttonObj = FindChild(buttonContainer, "TextButton");
            if (buttonObj != null)
            {
                var menuButton = buttonObj.GetComponent<MenuButton>();
                if (menuButton != null)
                {
                    // Set button type
                    menuButton.buttonType = UnityEngine.UI.MenuButton.MenuButtonType.Activate;

                    // Set click action via EventTrigger
                    var eventTrigger = buttonObj.GetComponent<EventTrigger>();
                    if (eventTrigger != null)
                    {
                        eventTrigger.triggers.Clear();
                        var entry = new EventTrigger.Entry
                        {
                            eventID = EventTriggerType.Submit
                        };
                        entry.callback.AddListener((data) => onClick?.Invoke());
                        eventTrigger.triggers.Add(entry);
                    }
                }

                // Set text
                var textObj = FindChild(buttonObj, "Menu Button Text");
                if (textObj != null)
                {
                    var textComp = textObj.GetComponent<Text>();
                    if (textComp != null)
                    {
                        textComp.text = text;
                    }
                }
            }

            return buttonContainer;
        }

        /// <summary>
        /// Get the MenuButton component from a button container.
        /// </summary>
        public static MenuButton GetMenuButton(GameObject buttonContainer)
        {
            var buttonObj = FindChild(buttonContainer, "TextButton");
            return buttonObj?.GetComponent<MenuButton>();
        }

        #region Helper Methods

        private static void CleanupMenuScreenTemplate(GameObject template)
        {
            // Remove MenuButtonList
            var mbl = template.GetComponent<MenuButtonList>();
            if (mbl != null) Object.Destroy(mbl);

            // Remove title localization
            var titleObj = FindChild(template, "Title");
            if (titleObj != null)
            {
                DestroyLocalization(titleObj);
            }
        }

        private static void DestroyLocalization(GameObject obj)
        {
            var localize = obj.GetComponent<AutoLocalizeTextUI>();
            if (localize != null)
            {
                Object.Destroy(localize);
            }
        }

        private static void DestroyAllChildren(GameObject obj)
        {
            for (int i = obj.transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(obj.transform.GetChild(i).gameObject);
            }
        }

        public static GameObject FindChild(GameObject parent, string path)
        {
            if (parent == null) return null;

            var parts = path.Split('/');
            Transform current = parent.transform;

            foreach (var part in parts)
            {
                current = current.Find(part);
                if (current == null) return null;
            }

            return current.gameObject;
        }

        #endregion
    }

    /// <summary>
    /// Helper component to detect when GameObject is destroyed.
    /// </summary>
    public class OnDestroyCallback : MonoBehaviour
    {
        public event Action OnDestroyed;

        private void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }
    }
}
