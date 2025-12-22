using System;
using System.Collections.Generic;
using System.Reflection;
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
        private float _previousTimeScale = 1f;

        // Reflection cache for cursor fix
        private static FieldInfo _controllerPressedField;
        private static bool _reflectionInitialized = false;

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

            // Initialize reflection for cursor fix
            InitializeReflection();

            // Create windows (they will load their saved states)
            _windows = new List<BaseWindow>();

            _mainWindow = new MainWindow(this);
            _windows.Add(_mainWindow);

            _windows.Add(new PlayerWindow());
            _windows.Add(new WorldWindow());
            _windows.Add(new EnemiesWindow());
            _windows.Add(new ItemsWindow());
            _windows.Add(new KeybindsWindow());
            _windows.Add(new SettingsWindow());
            _windows.Add(new DebugInfoWindow());

            Plugin.Log.LogInfo("DebugMenuController initialized with " + _windows.Count + " windows");
        }

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;

            try
            {
                // Get the _controllerPressed field from InputHandler
                var inputHandlerType = typeof(InputHandler);
                _controllerPressedField = inputHandlerType.GetField(
                    "_controllerPressed",
                    BindingFlags.NonPublic | BindingFlags.Static
                );

                if (_controllerPressedField != null)
                {
                    Plugin.Log.LogInfo("Found InputHandler._controllerPressed field for cursor fix");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not find _controllerPressed field: {e.Message}");
            }

            _reflectionInitialized = true;
        }

        private void Update()
        {
            // Update all windows (for keybind checks)
            foreach (var window in _windows)
            {
                window.Update();
            }
        }

        private void LateUpdate()
        {
            // Force cursor visibility every frame while menu is open
            if (_isVisible)
            {
                ForceCursorVisible();
            }
        }

        /// <summary>
        /// Force the cursor to be visible by manipulating game's internal state.
        /// </summary>
        private void ForceCursorVisible()
        {
            // Method 1: Direct cursor control
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Method 2: Set _controllerPressed = false via reflection
            // This makes InputHandler.SetCursorVisible(!_controllerPressed) show cursor
            try
            {
                if (_controllerPressedField != null)
                {
                    _controllerPressedField.SetValue(null, false);
                }
            }
            catch { }

            // Method 3: Enable UI mouse input
            try
            {
                var uiManager = UIManager.instance;
                if (uiManager?.inputModule != null)
                {
                    uiManager.inputModule.allowMouseInput = true;
                }
            }
            catch { }
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            // Force cursor visible during OnGUI as well
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

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

            // Save states
            _previousCursorVisible = Cursor.visible;
            _previousCursorLockState = Cursor.lockState;
            _previousTimeScale = Time.timeScale;

            // Force cursor visible
            ForceCursorVisible();

            // Pause game if option enabled
            if (DebugMenuConfig.PauseGameOnMenu)
            {
                Time.timeScale = 0f;
            }

            // Restore all windows to their saved visibility states
            foreach (var window in _windows)
            {
                // Main window always shows, others restore their saved state
                if (window == _mainWindow)
                {
                    window.IsVisible = true;
                }
                else
                {
                    // Load saved visibility from config
                    var state = DebugMenuConfig.GetWindowState(window.WindowId);
                    window.IsVisible = state.IsVisible;
                }
            }

            Plugin.Log.LogInfo("Debug menu opened");
        }

        /// <summary>
        /// Hide the debug menu.
        /// </summary>
        public void HideMenu()
        {
            if (!_isVisible) return;

            _isVisible = false;

            // Save all window states BEFORE hiding (to preserve current visibility)
            foreach (var window in _windows)
            {
                window.SaveState();
            }

            // Restore cursor state
            Cursor.visible = _previousCursorVisible;
            Cursor.lockState = _previousCursorLockState;

            // Always restore time scale if we paused it
            if (DebugMenuConfig.PauseGameOnMenu && Time.timeScale == 0f)
            {
                Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;
            }

            // Hide all windows WITHOUT saving (already saved above)
            foreach (var window in _windows)
            {
                window.IsVisible = false;  // Just set visibility, don't call Hide() which saves
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

