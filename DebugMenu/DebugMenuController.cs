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
        #region Private Fields

        /// <summary>Whether the debug menu is currently visible.</summary>
        private bool _isVisible = false;
        /// <summary>Previous cursor visibility state.</summary>
        private bool _previousCursorVisible;
        /// <summary>Previous cursor lock state.</summary>
        private CursorLockMode _previousCursorLockState;
        /// <summary>Previous time scale.</summary>
        private float _previousTimeScale = 1f;
        /// <summary>Tracks if we paused the game.</summary>
        private bool _pausedByUs = false;

        /// <summary>Cached field for cursor fix via reflection.</summary>
        private static FieldInfo _controllerPressedField;
        /// <summary>Whether reflection has been initialized.</summary>
        private static bool _reflectionInitialized = false;

        /// <summary>List of all debug windows.</summary>
        private List<BaseWindow> _windows;
        /// <summary>Reference to the main window.</summary>
        private MainWindow _mainWindow;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the debug menu is currently open.
        /// </summary>
        public bool IsVisible => _isVisible;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            DebugMenuConfig.Initialize(Plugin.ModConfig.ConfigFile);
            InitializeReflection();

            _windows = new List<BaseWindow>();

            _mainWindow = new MainWindow(this);
            _windows.Add(_mainWindow);

            _windows.Add(new PlayerWindow());
            _windows.Add(new WorldWindow());
            _windows.Add(new EnemiesWindow());
            _windows.Add(new InventoryWindow());
            _windows.Add(new KeybindsWindow());
            _windows.Add(new SettingsWindow());
            _windows.Add(new DebugInfoWindow());
            _windows.Add(new CombatWindow());
            _windows.Add(new HitboxWindow());
            _windows.Add(new SaveStateWindow());
            _windows.Add(new SpeedControlWindow());

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
            foreach (var window in _windows)
            {
                window.Update();
            }
        }

        private void LateUpdate()
        {
            if (_isVisible)
            {
                ForceCursorVisible();
            }
        }

        #endregion

        #region Cursor Control

        /// <summary>
        /// Force the cursor to be visible by manipulating game's internal state.
        /// </summary>
        private void ForceCursorVisible()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            try
            {
                if (_controllerPressedField != null)
                {
                    _controllerPressedField.SetValue(null, false);
                }
            }
            catch { }

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

        #endregion

        #region OnGUI

        private void OnGUI()
        {
            if (!_isVisible) return;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            DebugMenuStyles.EnsureInitialized();

            foreach (var window in _windows)
            {
                window.Draw();
            }
        }

        #endregion

        #region Menu Visibility

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

            _previousCursorVisible = Cursor.visible;
            _previousCursorLockState = Cursor.lockState;
            _previousTimeScale = Time.timeScale;

            ForceCursorVisible();

            if (DebugMenuConfig.PauseGameOnMenu)
            {
                _pausedByUs = true;
                Time.timeScale = 0f;
            }
            else
            {
                _pausedByUs = false;
            }

            foreach (var window in _windows)
            {
                if (window == _mainWindow)
                {
                    window.IsVisible = true;
                }
                else
                {
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

            foreach (var window in _windows)
            {
                window.SaveState();
            }

            Cursor.visible = _previousCursorVisible;
            Cursor.lockState = _previousCursorLockState;

            if (_pausedByUs && Time.timeScale == 0f)
            {
                Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;
                _pausedByUs = false;
            }

            foreach (var window in _windows)
            {
                window.IsVisible = false;
            }

            Plugin.Log.LogInfo("Debug menu closed");
        }

        #endregion

        #region Window Access

        /// <summary>
        /// Gets a window by type.
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

        #endregion
    }
}

