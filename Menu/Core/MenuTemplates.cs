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
        private static GameObject _sliderTemplate;
        private static GameObject _toggleTemplate;

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
                CleanupMenuScreenTemplate(_menuScreenTemplate);

                // Clone text button template from options screen
                var sampleButton = FindChild(optionsScreen, "Content/GameOptions");
                if (sampleButton != null)
                {
                    _textButtonTemplate = Object.Instantiate(sampleButton);
                    _textButtonTemplate.SetActive(false);
                    _textButtonTemplate.name = "TextButtonTemplate";
                    Object.DontDestroyOnLoad(_textButtonTemplate);

                    var buttonChild = FindChild(_textButtonTemplate, "GameOptionsButton");
                    if (buttonChild != null)
                    {
                        buttonChild.name = "TextButton";
                        DestroyLocalization(buttonChild);
                    }
                }

                // Clone slider template from AudioMenuScreen
                var audioScreen = FindChild(_uiCanvas, "AudioMenuScreen");
                if (audioScreen != null)
                {
                    var masterVolume = FindChild(audioScreen, "Content/MasterVolume");
                    if (masterVolume != null)
                    {
                        _sliderTemplate = Object.Instantiate(masterVolume);
                        _sliderTemplate.SetActive(false);
                        _sliderTemplate.name = "SliderTemplate";
                        Object.DontDestroyOnLoad(_sliderTemplate);

                        var sliderChild = FindChild(_sliderTemplate, "MasterSlider");
                        if (sliderChild != null)
                        {
                            sliderChild.name = "Slider";
                            // Remove audio-specific components
                            var audioSlider = sliderChild.GetComponent<MenuAudioSlider>();
                            if (audioSlider != null) Object.Destroy(audioSlider);

                            // Reset slider events
                            var slider = sliderChild.GetComponent<Slider>();
                            if (slider != null)
                            {
                                slider.onValueChanged = new Slider.SliderEvent();
                            }

                            DestroyLocalization(FindChild(sliderChild, "Menu Option Label"));
                        }

                        // Rename value text
                        var valueText = FindChild(_sliderTemplate, "MasterSlider/MasterVolValue");
                        if (valueText != null)
                        {
                            valueText.name = "Value";
                        }
                    }
                }

                // Clone toggle (choice) template from GameOptionsMenuScreen
                var gameOptionsScreen = FindChild(_uiCanvas, "GameOptionsMenuScreen");
                if (gameOptionsScreen != null)
                {
                    var camShake = FindChild(gameOptionsScreen, "Content/CamShakeSetting");
                    if (camShake != null)
                    {
                        _toggleTemplate = Object.Instantiate(camShake);
                        _toggleTemplate.SetActive(false);
                        _toggleTemplate.name = "ToggleTemplate";
                        Object.DontDestroyOnLoad(_toggleTemplate);

                        var optionChild = FindChild(_toggleTemplate, "CamShakePopupOption");
                        if (optionChild != null)
                        {
                            optionChild.name = "Toggle";
                            // Remove setting-specific components
                            var menuSetting = optionChild.GetComponent<MenuSetting>();
                            if (menuSetting != null) Object.Destroy(menuSetting);

                            var moh = optionChild.GetComponent<MenuOptionHorizontal>();
                            if (moh != null)
                            {
                                moh.optionList = new string[] { "Off", "On" };
                                moh.menuSetting = null;
                                moh.localizeText = false;
                                moh.applyButton = null;
                            }

                            DestroyLocalization(FindChild(optionChild, "Menu Option Label"));
                        }
                    }
                }

                // Clear content pane in template
                var contentPane = FindChild(_menuScreenTemplate, "Content");
                if (contentPane != null)
                {
                    DestroyAllChildren(contentPane);
                    var vlg = contentPane.GetComponent<VerticalLayoutGroup>();
                    if (vlg != null) Object.Destroy(vlg);
                    var mbl = contentPane.GetComponent<MenuButtonList>();
                    if (mbl != null) Object.Destroy(mbl);
                }

                // Subscribe to UIManager destruction
                uiManagerGO.AddComponent<OnDestroyCallback>().OnDestroyed += () =>
                {
                    _initialized = false;
                    SafeDestroy(ref _menuScreenTemplate);
                    SafeDestroy(ref _textButtonTemplate);
                    SafeDestroy(ref _sliderTemplate);
                    SafeDestroy(ref _toggleTemplate);
                    _uiCanvas = null;
                };

                _initialized = true;
                Plugin.Log.LogInfo($"MenuTemplates initialized (slider: {_sliderTemplate != null}, toggle: {_toggleTemplate != null})");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"MenuTemplates.Initialize failed: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void SafeDestroy(ref GameObject obj)
        {
            if (obj != null) Object.Destroy(obj);
            obj = null;
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

            var titleObj = FindChild(screen, "Title");
            if (titleObj != null)
            {
                var titleText = titleObj.GetComponent<Text>();
                if (titleText != null) titleText.text = title;
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
                Plugin.Log.LogError("MenuTemplates not initialized (button)");
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
                    menuButton.buttonType = UnityEngine.UI.MenuButton.MenuButtonType.Activate;

                    var eventTrigger = buttonObj.GetComponent<EventTrigger>();
                    if (eventTrigger != null)
                    {
                        eventTrigger.triggers.Clear();
                        var entry = new EventTrigger.Entry { eventID = EventTriggerType.Submit };
                        entry.callback.AddListener((data) => onClick?.Invoke());
                        eventTrigger.triggers.Add(entry);
                    }
                }

                var textObj = FindChild(buttonObj, "Menu Button Text");
                if (textObj != null)
                {
                    var textComp = textObj.GetComponent<Text>();
                    if (textComp != null) textComp.text = text;
                }
            }

            return buttonContainer;
        }

        /// <summary>
        /// Create a slider element with label and value display.
        /// </summary>
        public static GameObject CreateSlider(string label, float minValue, float maxValue, float currentValue,
            Action<float> onValueChanged, Func<float, string> formatValue = null)
        {
            if (!_initialized || _sliderTemplate == null)
            {
                Plugin.Log.LogError("MenuTemplates not initialized (slider)");
                return null;
            }

            var container = Object.Instantiate(_sliderTemplate);
            container.name = $"Slider_{label}";
            container.SetActive(true);

            var sliderObj = FindChild(container, "Slider");
            if (sliderObj != null)
            {
                var slider = sliderObj.GetComponent<Slider>();
                if (slider != null)
                {
                    slider.minValue = minValue;
                    slider.maxValue = maxValue;
                    slider.value = currentValue;
                    slider.wholeNumbers = false;

                    var valueText = FindChild(sliderObj, "Value")?.GetComponent<Text>();

                    // Update display
                    Action updateDisplay = () =>
                    {
                        if (valueText != null)
                        {
                            valueText.text = formatValue != null ? formatValue(slider.value) : $"{slider.value:F1}";
                        }
                    };
                    updateDisplay();

                    slider.onValueChanged.AddListener((v) =>
                    {
                        updateDisplay();
                        onValueChanged?.Invoke(v);
                    });
                }

                var labelText = FindChild(sliderObj, "Menu Option Label")?.GetComponent<Text>();
                if (labelText != null) labelText.text = label;
            }

            return container;
        }

        /// <summary>
        /// Create a toggle (on/off choice) element.
        /// </summary>
        public static GameObject CreateToggle(string label, bool currentValue, Action<bool> onValueChanged)
        {
            if (!_initialized || _toggleTemplate == null)
            {
                Plugin.Log.LogError("MenuTemplates not initialized (toggle)");
                return null;
            }

            var container = Object.Instantiate(_toggleTemplate);
            container.name = $"Toggle_{label}";
            container.SetActive(true);

            var toggleObj = FindChild(container, "Toggle");
            if (toggleObj != null)
            {
                var moh = toggleObj.GetComponent<MenuOptionHorizontal>();
                if (moh != null)
                {
                    moh.optionList = new string[] { "Off", "On" };
                    moh.selectedOptionIndex = currentValue ? 1 : 0;
                    moh.localizeText = false;

                    // Update text display
                    var choiceText = FindChild(toggleObj, "Menu Option Text")?.GetComponent<Text>();
                    if (choiceText != null)
                    {
                        choiceText.text = currentValue ? "On" : "Off";
                    }

                    // We need to hook into value changes - MenuOptionHorizontal doesn't have simple callback
                    // Add a helper component
                    var helper = toggleObj.AddComponent<ToggleHelper>();
                    helper.Initialize(moh, currentValue, (newValue) =>
                    {
                        if (choiceText != null) choiceText.text = newValue ? "On" : "Off";
                        onValueChanged?.Invoke(newValue);
                    });
                }

                var labelText = FindChild(toggleObj, "Menu Option Label")?.GetComponent<Text>();
                if (labelText != null) labelText.text = label;
            }

            return container;
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
            var mbl = template.GetComponent<MenuButtonList>();
            if (mbl != null) Object.Destroy(mbl);

            var titleObj = FindChild(template, "Title");
            if (titleObj != null) DestroyLocalization(titleObj);
        }

        private static void DestroyLocalization(GameObject obj)
        {
            if (obj == null) return;
            var localize = obj.GetComponent<AutoLocalizeTextUI>();
            if (localize != null) Object.Destroy(localize);
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

    /// <summary>
    /// Helper component for toggle value tracking.
    /// </summary>
    public class ToggleHelper : MonoBehaviour
    {
        private MenuOptionHorizontal _moh;
        private bool _currentValue;
        private Action<bool> _onChanged;

        public void Initialize(MenuOptionHorizontal moh, bool initialValue, Action<bool> onChanged)
        {
            _moh = moh;
            _currentValue = initialValue;
            _onChanged = onChanged;
        }

        private void Update()
        {
            if (_moh == null) return;

            bool newValue = _moh.selectedOptionIndex == 1;
            if (newValue != _currentValue)
            {
                _currentValue = newValue;
                _onChanged?.Invoke(newValue);
            }
        }
    }
}

