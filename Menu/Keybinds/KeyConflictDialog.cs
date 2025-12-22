using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace SilksongManager.Menu.Keybinds
{
    /// <summary>
    /// Dialog for handling keybind conflicts.
    /// Shows when user tries to set a key that's already used.
    /// </summary>
    public class KeyConflictDialog : MonoBehaviour
    {
        private GameObject _dialogRoot;
        private Text _messageText;
        private MenuButton _combineButton;
        private MenuButton _replaceButton;
        private MenuButton _cancelButton;
        
        private Action _onCombine;
        private Action _onReplace;
        private Action _onCancel;
        
        private bool _isVisible = false;
        
        /// <summary>
        /// Whether the dialog is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;
        
        /// <summary>
        /// Create the dialog from scratch.
        /// </summary>
        public static KeyConflictDialog Create(Transform parent)
        {
            // Create dialog container
            var dialogObj = new GameObject("KeyConflictDialog");
            dialogObj.transform.SetParent(parent, false);
            
            var dialog = dialogObj.AddComponent<KeyConflictDialog>();
            dialog.BuildUI();
            dialog.Hide();
            
            return dialog;
        }
        
        private void BuildUI()
        {
            _dialogRoot = gameObject;
            
            // Add CanvasGroup for fading
            var cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
            
            // Create background panel
            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // Semi-transparent background
            var bgImage = gameObject.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);
            
            // Dialog box
            var dialogBox = new GameObject("DialogBox");
            dialogBox.transform.SetParent(transform, false);
            var boxRect = dialogBox.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(600, 300);
            
            var boxBg = dialogBox.AddComponent<Image>();
            boxBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            
            // Message text
            var messageObj = new GameObject("Message");
            messageObj.transform.SetParent(dialogBox.transform, false);
            var msgRect = messageObj.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.1f, 0.55f);
            msgRect.anchorMax = new Vector2(0.9f, 0.9f);
            msgRect.offsetMin = Vector2.zero;
            msgRect.offsetMax = Vector2.zero;
            
            _messageText = messageObj.AddComponent<Text>();
            _messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _messageText.fontSize = 22;
            _messageText.alignment = TextAnchor.MiddleCenter;
            _messageText.color = Color.white;
            _messageText.text = "Key conflict detected";
            
            // Create buttons
            _combineButton = CreateButton(dialogBox.transform, "CombineBtn", "Combine", new Vector2(-150, -80), OnCombineClicked);
            _replaceButton = CreateButton(dialogBox.transform, "ReplaceBtn", "Replace", new Vector2(0, -80), OnReplaceClicked);
            _cancelButton = CreateButton(dialogBox.transform, "CancelBtn", "Cancel", new Vector2(150, -80), OnCancelClicked);
        }
        
        private MenuButton CreateButton(Transform parent, string name, string text, Vector2 position, UnityAction onClick)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(130, 50);
            rect.anchoredPosition = position;
            
            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.2f, 0.3f, 1f);
            
            // Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var btnText = textObj.AddComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 20;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.text = text;
            
            // Add Button component for click handling
            var button = btnObj.AddComponent<Button>();
            button.onClick.AddListener(onClick);
            
            // Add MenuButton for styling
            var menuBtn = btnObj.AddComponent<MenuButton>();
            menuBtn.OnSubmitPressed = new UnityEvent();
            menuBtn.OnSubmitPressed.AddListener(onClick);
            
            return menuBtn;
        }
        
        /// <summary>
        /// Show the conflict dialog.
        /// </summary>
        public void Show(KeyCode key, string conflictingActionName, Action onCombine, Action onReplace, Action onCancel)
        {
            _onCombine = onCombine;
            _onReplace = onReplace;
            _onCancel = onCancel;
            
            string keyName = key.ToString();
            _messageText.text = $"Key [{keyName}] is already used for:\n\n<b>{conflictingActionName}</b>\n\nCombine actions on same key, or replace?";
            
            _dialogRoot.SetActive(true);
            _isVisible = true;
            
            // Focus first button
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(_combineButton.gameObject);
            
            Plugin.Log.LogInfo($"Showing conflict dialog for {key} vs {conflictingActionName}");
        }
        
        /// <summary>
        /// Hide the dialog.
        /// </summary>
        public void Hide()
        {
            _dialogRoot.SetActive(false);
            _isVisible = false;
        }
        
        private void OnCombineClicked()
        {
            Hide();
            _onCombine?.Invoke();
        }
        
        private void OnReplaceClicked()
        {
            Hide();
            _onReplace?.Invoke();
        }
        
        private void OnCancelClicked()
        {
            Hide();
            _onCancel?.Invoke();
        }
        
        private void Update()
        {
            // Handle Escape to cancel
            if (_isVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                OnCancelClicked();
            }
        }
    }
}
