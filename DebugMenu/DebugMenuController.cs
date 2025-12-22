using System.Collections.Generic;
using UnityEngine;
using SilksongManager.DebugMenu.Windows;
using SilksongManager.Menu.Keybinds;

namespace SilksongManager.DebugMenu
{
    /// <summary>
    /// Main controller for the debug menu system.
    /// Handles visibility, cursor control, and window management.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class DebugMenuController : MonoBehaviour
    {
        private bool _isVisible = false;
        private bool _previousCursorVisible;
        private CursorLockMode _previousCursorLockState;

        private List<BaseWindow> _windows;
        private MainWindow _mainWindow;

        /// <summary>
        /// Whether the debug menu is currently open.
        /// </summary>
        public bool IsVisible => _isVisible;

        private void Awake()
        {
            // Initialize config
            DebugMenuConfig.Initialize(Plugin.ModConfig.ConfigFile);

            // Create windows
            _windows = new List<BaseWindow>();

            _mainWindow = new MainWindow(this);
            _windows.Add(_mainWindow);

            _windows.Add(new PlayerWindow());
            _windows.Add(new WorldWindow());
            _windows.Add(new EnemiesWindow());
            _windows.Add(new ItemsWindow());
            _windows.Add(new KeybindsWindow());
            _windows.Add(new SettingsWindow());

            Plugin.Log.LogInfo("DebugMenuController initialized with " + _windows.Count + " windows");
        }

        private void Update()
        {
            // Update all windows (for keybind checks)
            foreach (var window in _windows)
            {
                window.Update();
            }
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            // Ensure styles are initialized
            DebugMenuStyles.EnsureInitialized();

            // Draw all visible windows
            foreach (var window in _windows)
            {
                window.Draw();
            }
        }

        /// <summary>
        /// Toggle the menu visibility.
        /// </summary>
        public void ToggleMenu()
        {
            if (_isVisible)
            {
                HideMenu();
            }
            else
            {
                ShowMenu();
            }
        }

        /// <summary>
        /// Show the debug menu.
        /// </summary>
        public void ShowMenu()
        {
            if (_isVisible) return;

            _isVisible = true;

            // Save cursor state
            _previousCursorVisible = Cursor.visible;
            _previousCursorLockState = Cursor.lockState;

            // Show cursor
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Show main window
            _mainWindow?.Show();

            Plugin.Log.LogInfo("Debug menu opened");
        }

        /// <summary>
        /// Hide the debug menu.
        /// </summary>
        public void HideMenu()
        {
            if (!_isVisible) return;

            _isVisible = false;

            // Restore cursor state
            Cursor.visible = _previousCursorVisible;
            Cursor.lockState = _previousCursorLockState;

            // Hide all windows
            foreach (var window in _windows)
            {
                window.Hide();
            }

            Plugin.Log.LogInfo("Debug menu closed");
        }

        /// <summary>
        /// Get a window by type.
        /// </summary>
        public T GetWindow<T>() where T : BaseWindow
        {
            foreach (var window in _windows)
            {
                if (window is T typed)
                    return typed;
            }
            return null;
        }

        /// <summary>
        /// Toggle a window by type.
        /// </summary>
        public void ToggleWindow<T>() where T : BaseWindow
        {
            GetWindow<T>()?.Toggle();
        }
    }
}
