using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GlobalEnums;

namespace SilksongManager.Menu.Core
{
    /// <summary>
    /// Stack-based navigation system for custom menu screens.
    /// Provides proper back-button support and screen history.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class MenuNavigation
    {
        private static readonly Stack<CustomMenuScreen> _history = new Stack<CustomMenuScreen>();
        private static (MainMenuState State, MenuScreen Screen)? _baseMenuState;
        private static bool _isTransitioning = false;

        /// <summary>
        /// Current active custom screen, or null if at a base game menu.
        /// </summary>
        public static CustomMenuScreen CurrentScreen => _history.Count > 0 ? _history.Peek() : null;

        /// <summary>
        /// Whether we have any custom screens in history.
        /// </summary>
        public static bool HasHistory => _history.Count > 0;

        /// <summary>
        /// Navigate to a custom menu screen.
        /// </summary>
        public static void Show(CustomMenuScreen screen)
        {
            if (screen == null || _isTransitioning) return;

            var ui = UIManager.instance;
            if (ui == null) return;

            // If first custom screen, save current game menu state
            if (_history.Count == 0)
            {
                CaptureBaseMenuState();
            }

            // Check if already showing this screen
            if (_history.Count > 0 && _history.Peek() == screen)
                return;

            ui.StartCoroutine(ShowCoroutine(screen));
        }

        private static IEnumerator ShowCoroutine(CustomMenuScreen screen)
        {
            _isTransitioning = true;

            var ui = UIManager.instance;
            CustomMenuScreen previousScreen = _history.Count > 0 ? _history.Peek() : null;

            try
            {
                // Hide current screen
                if (previousScreen != null)
                {
                    previousScreen.InvokeOnHide(NavigationType.Forwards);
                    yield return ui.StartCoroutine(ui.HideMenu(previousScreen.MenuScreen));
                }
                else
                {
                    yield return ui.StartCoroutine(ui.HideCurrentMenu());
                }

                // Show new screen
                screen.InvokeOnShow(NavigationType.Forwards);
                yield return ui.StartCoroutine(ui.ShowMenu(screen.MenuScreen));

                // Push to history
                _history.Push(screen);
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        /// <summary>
        /// Go back to previous screen.
        /// </summary>
        public static void GoBack(int count = 1)
        {
            if (_history.Count == 0 || _isTransitioning) return;

            var ui = UIManager.instance;
            if (ui == null) return;

            ui.StartCoroutine(GoBackCoroutine(count));
        }

        private static IEnumerator GoBackCoroutine(int count)
        {
            _isTransitioning = true;

            var ui = UIManager.instance;
            CustomMenuScreen currentScreen = _history.Count > 0 ? _history.Peek() : null;

            try
            {
                // Hide current screen
                if (currentScreen != null)
                {
                    currentScreen.InvokeOnHide(NavigationType.Backwards);
                    yield return ui.StartCoroutine(ui.HideMenu(currentScreen.MenuScreen));
                }

                // Pop screens from history
                for (int i = 0; i < count && _history.Count > 0; i++)
                {
                    _history.Pop();
                }

                // Show previous screen or base game menu
                if (_history.Count > 0)
                {
                    var prevScreen = _history.Peek();
                    prevScreen.InvokeOnShow(NavigationType.Backwards);
                    yield return ui.StartCoroutine(ui.ShowMenu(prevScreen.MenuScreen));
                }
                else if (_baseMenuState.HasValue)
                {
                    var (state, screen) = _baseMenuState.Value;
                    _baseMenuState = null;
                    yield return ui.StartCoroutine(ui.ShowMenu(screen));
                    ui.menuState = state;
                }
                else
                {
                    // Fallback to options menu
                    Plugin.Log.LogWarning("MenuNavigation: No base state, returning to options");
                    yield return ui.StartCoroutine(ui.ShowMenu(ui.optionsMenuScreen));
                    ui.menuState = MainMenuState.OPTIONS_MENU;
                }
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        /// <summary>
        /// Go back to the base game menu (clear all history).
        /// </summary>
        public static void GoBackToRoot()
        {
            GoBack(_history.Count);
        }

        /// <summary>
        /// Handle back button press from current screen.
        /// </summary>
        public static void HandleBackPressed()
        {
            if (_history.Count == 0) return;

            var currentScreen = _history.Peek();
            currentScreen.InvokeOnBackPressed();

            if (currentScreen.AllowGoBack)
            {
                GoBack();
            }
        }

        /// <summary>
        /// Clear navigation state (call when leaving menu scene).
        /// </summary>
        public static void Reset()
        {
            if (_history.Count > 0)
            {
                var currentScreen = _history.Peek();
                currentScreen.InvokeOnHide(NavigationType.Backwards);
            }

            _history.Clear();
            _baseMenuState = null;
            _isTransitioning = false;
        }

        private static void CaptureBaseMenuState()
        {
            var ui = UIManager.instance;
            if (ui == null) return;

            var state = ui.menuState;
            var screen = GetActiveMenuScreen(ui);

            if (screen != null)
            {
                _baseMenuState = (state, screen);
            }
        }

        private static MenuScreen GetActiveMenuScreen(UIManager ui)
        {
            // Note: mainMenuScreen is CanvasGroup, not MenuScreen
            // So we can only reliably fall back to optionsMenuScreen
            try
            {
                if (ui.optionsMenuScreen != null && ui.optionsMenuScreen.gameObject.activeInHierarchy)
                    return ui.optionsMenuScreen;

                // Try reflection to get active screen if method exists
                var method = typeof(UIManager).GetMethod("GetActiveMenuScreen");
                if (method != null)
                {
                    return method.Invoke(ui, null) as MenuScreen;
                }
            }
            catch { }

            return ui.optionsMenuScreen; // Fallback
        }
    }

    /// <summary>
    /// Navigation direction enum for events.
    /// </summary>
    public enum NavigationType
    {
        Forwards,
        Backwards
    }
}
