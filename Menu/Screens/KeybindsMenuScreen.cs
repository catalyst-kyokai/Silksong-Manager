using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SilksongManager.Menu.Core;

namespace SilksongManager.Menu.Screens
{
    /// <summary>
    /// Keybinds configuration screen using new menu system.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class KeybindsMenuScreen : CustomMenuScreen
    {
        private Dictionary<Keybinds.ModAction, GameObject> _keybindButtons = new Dictionary<Keybinds.ModAction, GameObject>();
        private Keybinds.ModAction? _waitingForKey = null;
        private Text _waitingButtonText = null;

        public KeybindsMenuScreen() : base("Keybinds")
        {
        }

        protected override void BuildContent()
        {
            // Add vertical layout to content
            var vlg = ContentPane.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;

            // Add scroll view for many keybinds
            // For simplicity, just add buttons for now

            foreach (Keybinds.ModAction action in Enum.GetValues(typeof(Keybinds.ModAction)))
            {
                var actionName = Keybinds.ModKeybindManager.GetActionName(action);
                var currentKey = Keybinds.ModKeybindManager.GetKeybind(action);

                var buttonText = $"{actionName}: [{currentKey}]";
                var button = AddButton(buttonText, () => StartKeyCapture(action));

                _keybindButtons[action] = button;
            }
        }

        private void StartKeyCapture(Keybinds.ModAction action)
        {
            if (_waitingForKey.HasValue)
            {
                // Already waiting, cancel
                CancelKeyCapture();
            }

            _waitingForKey = action;

            // Update button text
            if (_keybindButtons.TryGetValue(action, out var buttonGO))
            {
                var textButton = MenuTemplates.FindChild(buttonGO, "TextButton/Menu Button Text");
                if (textButton != null)
                {
                    _waitingButtonText = textButton.GetComponent<Text>();
                    if (_waitingButtonText != null)
                    {
                        _waitingButtonText.text = $"{Keybinds.ModKeybindManager.GetActionName(action)}: [Press any key...]";
                    }
                }
            }

            // Start listening for key
            Container.AddComponent<KeyCaptureHelper>().OnKeyCaptured += OnKeyCaptured;
        }

        private void OnKeyCaptured(KeyCode key)
        {
            if (!_waitingForKey.HasValue) return;

            var action = _waitingForKey.Value;

            // Check for Escape to cancel
            if (key == KeyCode.Escape)
            {
                CancelKeyCapture();
                return;
            }

            // Set new keybind
            Keybinds.ModKeybindManager.SetKeybind(action, key);

            // Update button text
            RefreshButton(action);

            // Clean up
            _waitingForKey = null;
            _waitingButtonText = null;

            var helper = Container.GetComponent<KeyCaptureHelper>();
            if (helper != null) UnityEngine.Object.Destroy(helper);
        }

        private void CancelKeyCapture()
        {
            if (!_waitingForKey.HasValue) return;

            RefreshButton(_waitingForKey.Value);

            _waitingForKey = null;
            _waitingButtonText = null;

            var helper = Container.GetComponent<KeyCaptureHelper>();
            if (helper != null) UnityEngine.Object.Destroy(helper);
        }

        private void RefreshButton(Keybinds.ModAction action)
        {
            if (!_keybindButtons.TryGetValue(action, out var buttonGO)) return;

            var textButton = MenuTemplates.FindChild(buttonGO, "TextButton/Menu Button Text");
            if (textButton != null)
            {
                var text = textButton.GetComponent<Text>();
                if (text != null)
                {
                    var actionName = Keybinds.ModKeybindManager.GetActionName(action);
                    var currentKey = Keybinds.ModKeybindManager.GetKeybind(action);
                    text.text = $"{actionName}: [{currentKey}]";
                }
            }
        }

        private void RefreshAllButtons()
        {
            foreach (var action in _keybindButtons.Keys)
            {
                RefreshButton(action);
            }
        }

        protected override void OnBack()
        {
            // Cancel any pending key capture
            CancelKeyCapture();
        }

        protected override void OnDispose()
        {
            _keybindButtons.Clear();
        }
    }

    /// <summary>
    /// Helper component to capture next key press.
    /// </summary>
    public class KeyCaptureHelper : MonoBehaviour
    {
        public event Action<KeyCode> OnKeyCaptured;

        private void Update()
        {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    OnKeyCaptured?.Invoke(key);
                    return;
                }
            }
        }
    }
}
