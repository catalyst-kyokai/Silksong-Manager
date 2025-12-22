using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace SilksongManager.Menu.Keybinds
{
    /// <summary>
    /// UI component for a single mod keybind entry.
    /// Displays action name and current key, allows rebinding on click.
    /// </summary>
    public class ModMappableKey : MenuButton, ISubmitHandler, IEventSystemHandler, IPointerClickHandler, ICancelHandler
    {
        private ModAction _action;
        private Text _labelText;
        private Text _keyText;
        private Image _keyBackground;
        
        private bool _isListening = false;
        private string _oldKeyText;
        
        private Action<ModMappableKey, KeyCode> _onKeySelected;
        
        /// <summary>
        /// The mod action this key maps to.
        /// </summary>
        public ModAction Action => _action;
        
        /// <summary>
        /// Whether currently listening for a new key press.
        /// </summary>
        public bool IsListening => _isListening;
        
        /// <summary>
        /// Initialize this mappable key for a specific action.
        /// </summary>
        public void Initialize(ModAction action, Text labelText, Text keyText, Image keyBg, Action<ModMappableKey, KeyCode> onKeySelected)
        {
            _action = action;
            _labelText = labelText;
            _keyText = keyText;
            _keyBackground = keyBg;
            _onKeySelected = onKeySelected;
            
            // Set label
            if (_labelText != null)
            {
                _labelText.text = ModKeybindManager.GetActionName(action);
            }
            
            ShowCurrentBinding();
        }
        
        /// <summary>
        /// Display the current keybind.
        /// </summary>
        public void ShowCurrentBinding()
        {
            if (_keyText == null) return;
            
            var key = ModKeybindManager.GetKeybind(_action);
            _keyText.text = KeyCodeToDisplayString(key);
        }
        
        /// <summary>
        /// Start listening for a new key press.
        /// </summary>
        public void StartListening()
        {
            if (_isListening) return;
            
            _isListening = true;
            _oldKeyText = _keyText?.text ?? "";
            
            if (_keyText != null)
            {
                _keyText.text = "Press key...";
            }
            
            base.interactable = false;
            
            // Start the listening coroutine
            StartCoroutine(ListenForKeyPress());
            
            Plugin.Log.LogInfo($"Listening for new keybind for {_action}");
        }
        
        /// <summary>
        /// Stop listening and revert to previous binding.
        /// </summary>
        public void AbortListening()
        {
            if (!_isListening) return;
            
            _isListening = false;
            if (_keyText != null)
            {
                _keyText.text = _oldKeyText;
            }
            base.interactable = true;
            
            Plugin.Log.LogInfo($"Aborted rebind for {_action}");
        }
        
        /// <summary>
        /// Coroutine that listens for key press.
        /// </summary>
        private IEnumerator ListenForKeyPress()
        {
            // Wait a frame to avoid capturing the submit key
            yield return null;
            yield return null;
            
            while (_isListening)
            {
                // Check for escape to cancel
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    AbortListening();
                    yield break;
                }
                
                // Check all keycodes
                foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                {
                    // Skip some special keys
                    if (keyCode == KeyCode.None) continue;
                    if (keyCode == KeyCode.Mouse0 || keyCode == KeyCode.Mouse1 || keyCode == KeyCode.Mouse2) continue;
                    if (keyCode == KeyCode.Escape) continue; // Already handled
                    
                    if (Input.GetKeyDown(keyCode))
                    {
                        OnKeyDetected(keyCode);
                        yield break;
                    }
                }
                
                yield return null;
            }
        }
        
        /// <summary>
        /// Called when a key is detected.
        /// </summary>
        private void OnKeyDetected(KeyCode key)
        {
            _isListening = false;
            base.interactable = true;
            
            Plugin.Log.LogInfo($"Key detected for {_action}: {key}");
            
            // Notify parent to handle (may show conflict dialog)
            _onKeySelected?.Invoke(this, key);
        }
        
        /// <summary>
        /// Apply the new keybind (called after conflict resolution).
        /// </summary>
        public void ApplyKeybind(KeyCode key)
        {
            ModKeybindManager.SetKeybind(_action, key);
            ShowCurrentBinding();
        }
        
        /// <summary>
        /// Clear this keybind (set to None).
        /// </summary>
        public void ClearKeybind()
        {
            ModKeybindManager.SetKeybind(_action, KeyCode.None);
            ShowCurrentBinding();
        }
        
        public new void OnSubmit(BaseEventData eventData)
        {
            if (!_isListening)
            {
                StartListening();
            }
        }
        
        public new void OnPointerClick(PointerEventData eventData)
        {
            OnSubmit(eventData);
        }
        
        public new void OnCancel(BaseEventData eventData)
        {
            if (_isListening)
            {
                AbortListening();
            }
            else
            {
                base.OnCancel(eventData);
            }
        }
        
        /// <summary>
        /// Convert KeyCode to human-readable display string.
        /// </summary>
        private static string KeyCodeToDisplayString(KeyCode key)
        {
            if (key == KeyCode.None) return "---";
            
            // Handle some special cases for better display
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
                KeyCode.KeypadPlus => "Num+",
                KeyCode.KeypadMinus => "Num-",
                KeyCode.KeypadMultiply => "Num*",
                KeyCode.KeypadDivide => "Num/",
                KeyCode.KeypadEnter => "NumEnter",
                KeyCode.LeftShift => "LShift",
                KeyCode.RightShift => "RShift",
                KeyCode.LeftControl => "LCtrl",
                KeyCode.RightControl => "RCtrl",
                KeyCode.LeftAlt => "LAlt",
                KeyCode.RightAlt => "RAlt",
                KeyCode.UpArrow => "Up",
                KeyCode.DownArrow => "Down",
                KeyCode.LeftArrow => "Left",
                KeyCode.RightArrow => "Right",
                KeyCode.Return => "Enter",
                KeyCode.BackQuote => "`",
                KeyCode.Minus => "-",
                KeyCode.Equals => "=",
                KeyCode.LeftBracket => "[",
                KeyCode.RightBracket => "]",
                KeyCode.Backslash => "\\",
                KeyCode.Semicolon => ";",
                KeyCode.Quote => "'",
                KeyCode.Comma => ",",
                KeyCode.Period => ".",
                KeyCode.Slash => "/",
                _ => key.ToString()
            };
        }
    }
}
