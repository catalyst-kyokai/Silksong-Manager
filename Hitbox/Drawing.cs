using UnityEngine;

namespace SilksongManager.Hitbox
{
    /// <summary>
    /// Utility class for drawing 2D primitives using Unity's IMGUI system.
    /// Used by HitboxRenderer to draw hitbox outlines on screen.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class Drawing
    {
        /// <summary>
        /// Cached 1x1 white texture used for drawing primitives.
        /// </summary>
        private static Texture2D _whiteTexture;

        /// <summary>
        /// Gets or creates a 1x1 white texture for drawing operations.
        /// </summary>
        public static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                }
                return _whiteTexture;
            }
        }

        /// <summary>
        /// Draws a line between two points in GUI coordinates.
        /// </summary>
        /// <param name="pointA">Start point in GUI coordinates (origin at top-left).</param>
        /// <param name="pointB">End point in GUI coordinates (origin at top-left).</param>
        /// <param name="color">Color of the line.</param>
        /// <param name="width">Width of the line in pixels.</param>
        public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
        {
            float angle = Mathf.Atan2(pointB.y - pointA.y, pointB.x - pointA.x) * Mathf.Rad2Deg;
            float length = Vector2.Distance(pointA, pointB);

            Matrix4x4 savedMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pointA);
            GUI.color = color;
            GUI.DrawTexture(new Rect(pointA.x, pointA.y, length, width), WhiteTexture);
            GUI.color = Color.white;
            GUI.matrix = savedMatrix;
        }

        /// <summary>
        /// Draws an unfilled rectangle outline.
        /// </summary>
        /// <param name="rect">Rectangle bounds in GUI coordinates.</param>
        /// <param name="color">Color of the outline.</param>
        /// <param name="width">Width of the outline in pixels.</param>
        public static void DrawHollowRect(Rect rect, Color color, float width)
        {
            DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.x + rect.width, rect.y), color, width);
            DrawLine(new Vector2(rect.x, rect.y + rect.height), new Vector2(rect.x + rect.width, rect.y + rect.height), color, width);
            DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.x, rect.y + rect.height), color, width);
            DrawLine(new Vector2(rect.x + rect.width, rect.y), new Vector2(rect.x + rect.width, rect.y + rect.height), color, width);
        }

        /// <summary>
        /// Draws an unfilled circle outline using line segments.
        /// </summary>
        /// <param name="center">Center point of the circle in GUI coordinates.</param>
        /// <param name="radius">Radius of the circle in pixels.</param>
        /// <param name="color">Color of the circle outline.</param>
        /// <param name="width">Width of the outline in pixels.</param>
        /// <param name="segments">Number of line segments used to approximate the circle (default: 32).</param>
        public static void DrawCircle(Vector2 center, float radius, Color color, float width, int segments = 32)
        {
            float angleStep = 360f / segments;
            Vector2 prevPoint = center + new Vector2(radius, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector2 newPoint = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                DrawLine(prevPoint, newPoint, color, width);
                prevPoint = newPoint;
            }
        }
    }
}
