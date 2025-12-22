using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SilksongManager.Menu.Core
{
    /// <summary>
    /// Base class for custom menu screens.
    /// Provides container, content pane, back button, and lifecycle events.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public abstract class CustomMenuScreen
    {
        /// <summary>
        /// Root container GameObject for this screen.
        /// </summary>
        public GameObject Container { get; private set; }

        /// <summary>
        /// The MenuScreen component.
        /// </summary>
        public MenuScreen MenuScreen { get; private set; }

        /// <summary>
        /// Title text element.
        /// </summary>
        public Text TitleText { get; private set; }

        /// <summary>
        /// Content pane where menu elements are added.
        /// </summary>
        public GameObject ContentPane { get; private set; }

        /// <summary>
        /// Controls pane (contains back button).
        /// </summary>
        public GameObject ControlsPane { get; private set; }

        /// <summary>
        /// Back button at bottom of screen.
        /// </summary>
        public MenuButton BackButton { get; private set; }

        /// <summary>
        /// If false, back button won't navigate back.
        /// </summary>
        public bool AllowGoBack { get; set; } = true;

        /// <summary>
        /// Screen title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Event fired when screen is shown.
        /// </summary>
        public event Action<NavigationType> OnShow;

        /// <summary>
        /// Event fired when screen is hidden.
        /// </summary>
        public event Action<NavigationType> OnHide;

        /// <summary>
        /// Event fired when back button is pressed.
        /// </summary>
        public event Action OnBackPressed;

        protected CustomMenuScreen(string title)
        {
            Title = title;
            CreateScreen();
        }

        private void CreateScreen()
        {
            Container = MenuTemplates.CreateMenuScreen(Title);
            if (Container == null)
            {
                Plugin.Log.LogError($"CustomMenuScreen: Failed to create screen '{Title}'");
                return;
            }

            MenuScreen = Container.GetComponent<MenuScreen>();
            TitleText = MenuTemplates.FindChild(Container, "Title")?.GetComponent<Text>();
            ContentPane = MenuTemplates.FindChild(Container, "Content");
            ControlsPane = MenuTemplates.FindChild(Container, "Controls");

            // Add VerticalLayoutGroup to ContentPane for proper button stacking
            if (ContentPane != null)
            {
                var vlg = ContentPane.GetComponent<VerticalLayoutGroup>();
                if (vlg == null)
                {
                    vlg = ContentPane.AddComponent<VerticalLayoutGroup>();
                }
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.spacing = 20f;
                vlg.childControlWidth = false;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = false;
                vlg.childForceExpandHeight = false;
            }

            // Find and setup back button
            var applyBtn = MenuTemplates.FindChild(ControlsPane, "ApplyButton");
            if (applyBtn != null)
            {
                BackButton = applyBtn.GetComponent<MenuButton>();
                if (MenuScreen != null)
                {
                    MenuScreen.backButton = BackButton;
                }

                // Set back button click handler
                var eventTrigger = applyBtn.GetComponent<EventTrigger>();
                if (eventTrigger != null)
                {
                    eventTrigger.triggers.Clear();
                    var entry = new EventTrigger.Entry
                    {
                        eventID = EventTriggerType.Submit
                    };
                    entry.callback.AddListener((data) => MenuNavigation.HandleBackPressed());
                    eventTrigger.triggers.Add(entry);
                }

                // Set back button text
                var backText = MenuTemplates.FindChild(applyBtn, "Menu Button Text");
                if (backText != null)
                {
                    var textComp = backText.GetComponent<Text>();
                    if (textComp != null)
                    {
                        textComp.text = "Back";
                    }

                    // Remove localization
                    var localize = backText.GetComponent<AutoLocalizeTextUI>();
                    if (localize != null)
                    {
                        UnityEngine.Object.Destroy(localize);
                    }
                }
            }

            // Add destroy callback
            Container.AddComponent<OnDestroyCallback>().OnDestroyed += () =>
            {
                OnDispose();
            };

            // Build content
            BuildContent();
        }

        /// <summary>
        /// Override to add content elements to the screen.
        /// </summary>
        protected abstract void BuildContent();

        /// <summary>
        /// Add a text button to the content pane.
        /// </summary>
        protected GameObject AddButton(string text, Action onClick)
        {
            var button = MenuTemplates.CreateTextButton(text, onClick);
            if (button != null && ContentPane != null)
            {
                button.transform.SetParent(ContentPane.transform, false);
                button.SetActive(true);
            }
            return button;
        }

        /// <summary>
        /// Called when screen is being shown.
        /// </summary>
        internal void InvokeOnShow(NavigationType navType)
        {
            OnShow?.Invoke(navType);
            OnScreenShow(navType);
        }

        /// <summary>
        /// Called when screen is being hidden.
        /// </summary>
        internal void InvokeOnHide(NavigationType navType)
        {
            OnHide?.Invoke(navType);
            OnScreenHide(navType);
        }

        /// <summary>
        /// Called when back button is pressed.
        /// </summary>
        internal void InvokeOnBackPressed()
        {
            OnBackPressed?.Invoke();
            OnBack();
        }

        /// <summary>
        /// Override for custom show behavior.
        /// </summary>
        protected virtual void OnScreenShow(NavigationType navType) { }

        /// <summary>
        /// Override for custom hide behavior.
        /// </summary>
        protected virtual void OnScreenHide(NavigationType navType) { }

        /// <summary>
        /// Override for custom back button behavior.
        /// </summary>
        protected virtual void OnBack() { }

        /// <summary>
        /// Called when screen is destroyed.
        /// </summary>
        protected virtual void OnDispose() { }

        /// <summary>
        /// Destroy this screen.
        /// </summary>
        public void Dispose()
        {
            if (Container != null)
            {
                UnityEngine.Object.Destroy(Container);
                Container = null;
            }
        }
    }
}
