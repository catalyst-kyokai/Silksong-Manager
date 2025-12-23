using UnityEngine;
using System.Collections.Generic;

namespace SilksongManager.UI
{
    /// <summary>
    /// Manages on-screen notifications for keybind actions.
    /// Displays notifications in the top-right corner with smooth fade animations.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        #region Singleton

        private static NotificationManager _instance;
        public static NotificationManager Instance => _instance;

        #endregion

        #region Notification Data

        private class Notification
        {
            public string Title;
            public string Message;
            public float Duration;
            public float TimeRemaining;
            public float Alpha;
            public NotificationState State;
        }

        private enum NotificationState
        {
            FadingIn,
            Visible,
            FadingOut
        }

        #endregion

        #region Private Fields

        private readonly List<Notification> _notifications = new List<Notification>();
        private const float FADE_DURATION = 0.25f;
        private const float DEFAULT_DURATION = 2f;
        private const float NOTIFICATION_HEIGHT = 50f;
        private const float NOTIFICATION_WIDTH = 280f;
        private const float PADDING = 15f;
        private const float SPACING = 8f;
        private const int MAX_NOTIFICATIONS = 5;

        private GUIStyle _titleStyle;
        private GUIStyle _messageStyle;
        private GUIStyle _boxStyle;
        private Texture2D _backgroundTexture;
        private bool _stylesInitialized = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void Update()
        {
            UpdateNotifications();
        }

        private void OnGUI()
        {
            if (_notifications.Count == 0) return;

            InitializeStyles();
            DrawNotifications();
        }

        private void OnDestroy()
        {
            if (_backgroundTexture != null)
            {
                Destroy(_backgroundTexture);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows a notification with the specified title and message.
        /// </summary>
        /// <param name="title">The notification title (bold).</param>
        /// <param name="message">Optional additional message.</param>
        /// <param name="duration">How long to display (default 2 seconds).</param>
        public static void Show(string title, string message = null, float duration = DEFAULT_DURATION)
        {
            if (_instance == null)
            {
                Plugin.Log.LogWarning("NotificationManager not initialized");
                return;
            }

            _instance.AddNotification(title, message, duration);
        }

        #endregion

        #region Private Methods

        private void AddNotification(string title, string message, float duration)
        {
            // Remove oldest if at max
            while (_notifications.Count >= MAX_NOTIFICATIONS)
            {
                _notifications.RemoveAt(0);
            }

            var notification = new Notification
            {
                Title = title,
                Message = message,
                Duration = duration,
                TimeRemaining = duration,
                Alpha = 0f,
                State = NotificationState.FadingIn
            };

            _notifications.Add(notification);
            Plugin.Log.LogInfo($"[Notification] {title}: {message}");
        }

        private void UpdateNotifications()
        {
            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                var n = _notifications[i];

                switch (n.State)
                {
                    case NotificationState.FadingIn:
                        n.Alpha += Time.unscaledDeltaTime / FADE_DURATION;
                        if (n.Alpha >= 1f)
                        {
                            n.Alpha = 1f;
                            n.State = NotificationState.Visible;
                        }
                        break;

                    case NotificationState.Visible:
                        n.TimeRemaining -= Time.unscaledDeltaTime;
                        if (n.TimeRemaining <= 0f)
                        {
                            n.State = NotificationState.FadingOut;
                        }
                        break;

                    case NotificationState.FadingOut:
                        n.Alpha -= Time.unscaledDeltaTime / FADE_DURATION;
                        if (n.Alpha <= 0f)
                        {
                            _notifications.RemoveAt(i);
                        }
                        break;
                }
            }
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            // Create background texture
            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.12f, 0.95f));
            _backgroundTexture.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _backgroundTexture },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(12, 12, 8, 8)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.75f, 0.3f) }, // Gold color
                alignment = TextAnchor.MiddleLeft
            };

            _messageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.85f, 0.85f, 0.9f) },
                alignment = TextAnchor.MiddleLeft
            };

            _stylesInitialized = true;
        }

        private void DrawNotifications()
        {
            float yOffset = PADDING;

            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                var n = _notifications[i];

                // Calculate height based on content
                float height = string.IsNullOrEmpty(n.Message) ? 35f : NOTIFICATION_HEIGHT;

                // Position: top-right corner
                Rect rect = new Rect(
                    Screen.width - NOTIFICATION_WIDTH - PADDING,
                    yOffset,
                    NOTIFICATION_WIDTH,
                    height
                );

                // Apply alpha
                var oldColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, n.Alpha);

                // Draw background
                GUI.Box(rect, GUIContent.none, _boxStyle);

                // Draw title
                Rect titleRect = new Rect(rect.x + 12, rect.y + 6, rect.width - 24, 20);
                GUI.Label(titleRect, n.Title, _titleStyle);

                // Draw message if present
                if (!string.IsNullOrEmpty(n.Message))
                {
                    Rect messageRect = new Rect(rect.x + 12, rect.y + 26, rect.width - 24, 18);
                    GUI.Label(messageRect, n.Message, _messageStyle);
                }

                GUI.color = oldColor;

                yOffset += height + SPACING;
            }
        }

        #endregion
    }
}
