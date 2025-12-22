using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GlobalEnums;

namespace SilksongManager.Menu.Core
{
    /// <summary>
    /// Stack-based navigation system for custom menu screens.
    /// Properly handles main menu (CanvasGroup) vs other menus (MenuScreen).
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class MenuNavigation
    {
        private static readonly Stack<CustomMenuScreen> _history = new Stack<CustomMenuScreen>();
        private static MainMenuState _baseMenuState;
        private static bool _wasOnMainMenu = false;
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

            // If first custom screen, capture where we came from
            if (_history.Count == 0)
            {
                CaptureBaseMenuState(ui);
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
                else if (_wasOnMainMenu)
                {
                    // Hide main menu (CanvasGroup)
                    yield return ui.StartCoroutine(HideMainMenu(ui));
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
            Plugin.Log.LogInfo($"MenuNavigation.GoBack: count={count}, historyCount={_history.Count}, wasOnMainMenu={_wasOnMainMenu}");

            var ui = UIManager.instance;
            CustomMenuScreen currentScreen = _history.Count > 0 ? _history.Peek() : null;

            try
            {
                // Hide current screen
                if (currentScreen != null)
                {
                    Plugin.Log.LogInfo($"MenuNavigation: Hiding screen '{currentScreen.Title}'");
                    currentScreen.InvokeOnHide(NavigationType.Backwards);
                    yield return ui.StartCoroutine(ui.HideMenu(currentScreen.MenuScreen));
                }

                // Pop screens from history
                for (int i = 0; i < count && _history.Count > 0; i++)
                {
                    var popped = _history.Pop();
                    Plugin.Log.LogInfo($"MenuNavigation: Popped '{popped.Title}' from history, remaining={_history.Count}");
                }

                // Show previous screen or base game menu
                if (_history.Count > 0)
                {
                    var prevScreen = _history.Peek();
                    Plugin.Log.LogInfo($"MenuNavigation: Showing previous screen '{prevScreen.Title}'");
                    prevScreen.InvokeOnShow(NavigationType.Backwards);
                    yield return ui.StartCoroutine(ui.ShowMenu(prevScreen.MenuScreen));
                }
                else if (_wasOnMainMenu)
                {
                    Plugin.Log.LogInfo("MenuNavigation: Restoring main menu (CanvasGroup)");
                    yield return ui.StartCoroutine(ShowMainMenu(ui));
                    ui.menuState = _baseMenuState;
                    _wasOnMainMenu = false;
                    Plugin.Log.LogInfo("MenuNavigation: Main menu restored");
                }
                else
                {
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
            _wasOnMainMenu = false;
            _isTransitioning = false;
        }

        private static void CaptureBaseMenuState(UIManager ui)
        {
            _baseMenuState = ui.menuState;

            // Check if we're on main menu (CanvasGroup)
            if (ui.mainMenuScreen != null && ui.mainMenuScreen.gameObject.activeInHierarchy)
            {
                _wasOnMainMenu = true;
                Plugin.Log.LogInfo("MenuNavigation: Captured main menu state");
            }
            else
            {
                _wasOnMainMenu = false;
            }
        }

        /// <summary>
        /// Hide main menu (CanvasGroup) with fade - also hides gameTitle logo.
        /// </summary>
        private static IEnumerator HideMainMenu(UIManager ui)
        {
            var mainMenu = ui.mainMenuScreen;
            if (mainMenu == null) yield break;

            mainMenu.interactable = false;

            // Also hide the game title logo
            if (ui.gameTitle != null)
            {
                ui.gameTitle.gameObject.SetActive(false);
            }

            // Fade out
            float duration = 0.2f;
            float timer = 0f;
            float startAlpha = mainMenu.alpha;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                mainMenu.alpha = Mathf.Lerp(startAlpha, 0f, timer / duration);
                yield return null;
            }

            mainMenu.alpha = 0f;
            mainMenu.gameObject.SetActive(false);
        }

        /// <summary>
        /// Show main menu (CanvasGroup) with fade.
        /// </summary>
        private static IEnumerator ShowMainMenu(UIManager ui)
        {
            var mainMenu = ui.mainMenuScreen;
            if (mainMenu == null) yield break;

            mainMenu.gameObject.SetActive(true);
            mainMenu.alpha = 0f;

            // Also show the game title logo
            if (ui.gameTitle != null)
            {
                ui.gameTitle.gameObject.SetActive(true);
            }

            // Fade in
            float duration = 0.2f;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                mainMenu.alpha = Mathf.Lerp(0f, 1f, timer / duration);
                yield return null;
            }

            mainMenu.alpha = 1f;
            mainMenu.interactable = true;
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

