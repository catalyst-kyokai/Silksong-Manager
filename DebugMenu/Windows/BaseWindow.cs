using UnityEngine;

namespace SilksongManager.DebugMenu.Windows
{
    /// <summary>
    /// Abstract base class for all debug menu windows.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public abstract class BaseWindow
    {
        /// <summary>
        /// Unique window ID for GUI.Window.
        /// </summary>
        public abstract int WindowId { get; }
        
        /// <summary>
        /// Window title displayed in header.
        /// </summary>
        public abstract string Title { get; }
        
        /// <summary>
        /// Default window size.
        /// </summary>
        protected virtual Vector2 DefaultSize => new Vector2(280, 300);
        
        /// <summary>
        /// Minimum window size.
        /// </summary>
        protected virtual Vector2 MinSize => new Vector2(200, 150);
        
        /// <summary>
        /// Current window rect (position and size).
        /// </summary>
        public Rect WindowRect { get; set; }
        
        /// <summary>
        /// Whether window is currently visible.
        /// </summary>
        public bool IsVisible { get; set; }
        
        /// <summary>
        /// Whether window is detached from main menu.
        /// </summary>
        public bool IsDetached { get; set; }
        
        /// <summary>
        /// Optional keybind to toggle this window.
        /// </summary>
        public virtual KeyCode ToggleKey => KeyCode.None;
        
        /// <summary>
        /// Scroll position for scrollable content.
        /// </summary>
        protected Vector2 ScrollPosition { get; set; }
        
        protected BaseWindow()
        {
            var defaultPos = GetDefaultPosition();
            WindowRect = new Rect(defaultPos.x, defaultPos.y, DefaultSize.x, DefaultSize.y);
            IsVisible = false;
            IsDetached = false;
        }
        
        /// <summary>
        /// Get default position for this window type.
        /// </summary>
        protected virtual Vector2 GetDefaultPosition()
        {
            // Offset windows so they don't all stack on top of each other
            return new Vector2(20 + (WindowId % 5) * 30, 20 + (WindowId % 5) * 30);
        }
        
        /// <summary>
        /// Draw the window. Called from DebugMenuController.
        /// </summary>
        public void Draw()
        {
            if (!IsVisible) return;
            
            // Apply opacity
            var prevColor = GUI.color;
            var alpha = DebugMenuConfig.GetEffectiveAlpha();
            GUI.color = new Color(1, 1, 1, alpha);
            
            // Draw window
            WindowRect = GUILayout.Window(
                WindowId,
                WindowRect,
                DrawWindowInternal,
                "",  // Empty title - we draw custom header
                DebugMenuStyles.Window,
                GUILayout.MinWidth(MinSize.x),
                GUILayout.MinHeight(MinSize.y)
            );
            
            // Clamp to screen
            WindowRect = ClampToScreen(WindowRect);
            
            GUI.color = prevColor;
        }
        
        private void DrawWindowInternal(int id)
        {
            // Custom header with close button
            DrawHeader();
            
            // Scrollable content area
            ScrollPosition = GUILayout.BeginScrollView(ScrollPosition);
            
            DrawContent();
            
            GUILayout.EndScrollView();
            
            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, WindowRect.width, 24));
        }
        
        /// <summary>
        /// Draw window header with title and close button.
        /// </summary>
        protected virtual void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            
            GUILayout.Label(Title, DebugMenuStyles.Header, GUILayout.ExpandWidth(true));
            
            if (GUILayout.Button("×", DebugMenuStyles.CloseButton))
            {
                IsVisible = false;
            }
            
            GUILayout.EndHorizontal();
            
            DebugMenuStyles.DrawSeparator();
        }
        
        /// <summary>
        /// Draw the main window content. Override in derived classes.
        /// </summary>
        protected abstract void DrawContent();
        
        /// <summary>
        /// Called every frame. Use for input handling.
        /// </summary>
        public virtual void Update()
        {
            // Check for toggle key
            if (ToggleKey != KeyCode.None && Input.GetKeyDown(ToggleKey))
            {
                Toggle();
            }
        }
        
        /// <summary>
        /// Toggle window visibility.
        /// </summary>
        public void Toggle()
        {
            IsVisible = !IsVisible;
        }
        
        /// <summary>
        /// Show the window.
        /// </summary>
        public void Show()
        {
            IsVisible = true;
        }
        
        /// <summary>
        /// Hide the window.
        /// </summary>
        public void Hide()
        {
            IsVisible = false;
        }
        
        /// <summary>
        /// Clamp window rect to screen bounds.
        /// </summary>
        private Rect ClampToScreen(Rect rect)
        {
            rect.x = Mathf.Clamp(rect.x, 0, Screen.width - rect.width);
            rect.y = Mathf.Clamp(rect.y, 0, Screen.height - rect.height);
            return rect;
        }
    }
}
