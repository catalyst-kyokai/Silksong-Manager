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
        /// Maximum window size.
        /// </summary>
        protected virtual Vector2 MaxSize => new Vector2(800, 800);

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

        // Resize state
        private bool _isResizing = false;
        private ResizeDirection _resizeDir = ResizeDirection.None;
        private Vector2 _resizeStartPos;
        private Rect _resizeStartRect;
        private const float RESIZE_BORDER = 8f;

        private enum ResizeDirection
        {
            None,
            Right,
            Bottom,
            BottomRight,
            Left,
            Top,
            TopLeft,
            TopRight,
            BottomLeft
        }

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

            // Handle resizing before drawing window
            HandleResize();

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

            // Draw resize handle indicator
            DrawResizeHandle();

            // Clamp to screen
            WindowRect = ClampToScreen(WindowRect);

            GUI.color = prevColor;
        }

        private void HandleResize()
        {
            var mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            var e = Event.current;

            // Check for resize start
            if (!_isResizing && Input.GetMouseButtonDown(0))
            {
                _resizeDir = GetResizeDirection(mousePos);
                if (_resizeDir != ResizeDirection.None)
                {
                    _isResizing = true;
                    _resizeStartPos = mousePos;
                    _resizeStartRect = WindowRect;
                }
            }

            // Handle resize drag
            if (_isResizing && Input.GetMouseButton(0))
            {
                var delta = mousePos - _resizeStartPos;
                var newRect = _resizeStartRect;

                switch (_resizeDir)
                {
                    case ResizeDirection.Right:
                        newRect.width = Mathf.Clamp(_resizeStartRect.width + delta.x, MinSize.x, MaxSize.x);
                        break;
                    case ResizeDirection.Bottom:
                        newRect.height = Mathf.Clamp(_resizeStartRect.height + delta.y, MinSize.y, MaxSize.y);
                        break;
                    case ResizeDirection.BottomRight:
                        newRect.width = Mathf.Clamp(_resizeStartRect.width + delta.x, MinSize.x, MaxSize.x);
                        newRect.height = Mathf.Clamp(_resizeStartRect.height + delta.y, MinSize.y, MaxSize.y);
                        break;
                    case ResizeDirection.Left:
                        float newWidth = Mathf.Clamp(_resizeStartRect.width - delta.x, MinSize.x, MaxSize.x);
                        newRect.x = _resizeStartRect.x + (_resizeStartRect.width - newWidth);
                        newRect.width = newWidth;
                        break;
                    case ResizeDirection.Top:
                        float newHeight = Mathf.Clamp(_resizeStartRect.height - delta.y, MinSize.y, MaxSize.y);
                        newRect.y = _resizeStartRect.y + (_resizeStartRect.height - newHeight);
                        newRect.height = newHeight;
                        break;
                    case ResizeDirection.TopLeft:
                        float newW = Mathf.Clamp(_resizeStartRect.width - delta.x, MinSize.x, MaxSize.x);
                        float newH = Mathf.Clamp(_resizeStartRect.height - delta.y, MinSize.y, MaxSize.y);
                        newRect.x = _resizeStartRect.x + (_resizeStartRect.width - newW);
                        newRect.y = _resizeStartRect.y + (_resizeStartRect.height - newH);
                        newRect.width = newW;
                        newRect.height = newH;
                        break;
                    case ResizeDirection.TopRight:
                        newRect.width = Mathf.Clamp(_resizeStartRect.width + delta.x, MinSize.x, MaxSize.x);
                        float newH2 = Mathf.Clamp(_resizeStartRect.height - delta.y, MinSize.y, MaxSize.y);
                        newRect.y = _resizeStartRect.y + (_resizeStartRect.height - newH2);
                        newRect.height = newH2;
                        break;
                    case ResizeDirection.BottomLeft:
                        float newW2 = Mathf.Clamp(_resizeStartRect.width - delta.x, MinSize.x, MaxSize.x);
                        newRect.x = _resizeStartRect.x + (_resizeStartRect.width - newW2);
                        newRect.width = newW2;
                        newRect.height = Mathf.Clamp(_resizeStartRect.height + delta.y, MinSize.y, MaxSize.y);
                        break;
                }

                WindowRect = newRect;
            }

            // End resize
            if (_isResizing && Input.GetMouseButtonUp(0))
            {
                _isResizing = false;
                _resizeDir = ResizeDirection.None;
            }
        }

        private ResizeDirection GetResizeDirection(Vector2 mousePos)
        {
            var rect = WindowRect;
            bool inWindow = rect.Contains(mousePos);
            if (!inWindow) return ResizeDirection.None;

            bool onLeft = mousePos.x < rect.x + RESIZE_BORDER;
            bool onRight = mousePos.x > rect.xMax - RESIZE_BORDER;
            bool onTop = mousePos.y < rect.y + RESIZE_BORDER;
            bool onBottom = mousePos.y > rect.yMax - RESIZE_BORDER;

            if (onBottom && onRight) return ResizeDirection.BottomRight;
            if (onTop && onLeft) return ResizeDirection.TopLeft;
            if (onTop && onRight) return ResizeDirection.TopRight;
            if (onBottom && onLeft) return ResizeDirection.BottomLeft;
            if (onRight) return ResizeDirection.Right;
            if (onBottom) return ResizeDirection.Bottom;
            if (onLeft) return ResizeDirection.Left;
            if (onTop) return ResizeDirection.Top;

            return ResizeDirection.None;
        }

        private void DrawResizeHandle()
        {
            // Draw a small resize indicator in bottom-right corner
            var handleRect = new Rect(
                WindowRect.xMax - 16,
                WindowRect.yMax - 16,
                14,
                14
            );

            GUI.DrawTexture(handleRect, DebugMenuStyles.ButtonNormalTex);
        }

        private void DrawWindowInternal(int id)
        {
            // Custom header with close button
            DrawHeader();

            // Scrollable content area
            ScrollPosition = GUILayout.BeginScrollView(ScrollPosition);

            DrawContent();

            GUILayout.EndScrollView();

            // Make window draggable by title area
            GUI.DragWindow(new Rect(0, 0, WindowRect.width, 30));
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
            rect.x = Mathf.Clamp(rect.x, 0, Screen.width - 50);
            rect.y = Mathf.Clamp(rect.y, 0, Screen.height - 50);
            return rect;
        }
    }
}
