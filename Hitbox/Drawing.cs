using UnityEngine;

namespace SilksongManager.Hitbox
{
    public static class Drawing
    {
        private static Texture2D _whiteTexture;
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

        public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
        {
            // PointA and PointB must be in GUI coordinates (Top-Left 0,0)
            float angle = Mathf.Atan2(pointB.y - pointA.y, pointB.x - pointA.x) * Mathf.Rad2Deg;
            float length = Vector2.Distance(pointA, pointB);

            Matrix4x4 savedMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pointA);
            GUI.color = color;
            GUI.DrawTexture(new Rect(pointA.x, pointA.y, length, width), WhiteTexture);
            GUI.color = Color.white;
            GUI.matrix = savedMatrix;
        }

        public static void DrawHollowRect(Rect rect, Color color, float width)
        {
            DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.x + rect.width, rect.y), color, width);
            DrawLine(new Vector2(rect.x, rect.y + rect.height), new Vector2(rect.x + rect.width, rect.y + rect.height), color, width);
            DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.x, rect.y + rect.height), color, width);
            DrawLine(new Vector2(rect.x + rect.width, rect.y), new Vector2(rect.x + rect.width, rect.y + rect.height), color, width);
        }

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
