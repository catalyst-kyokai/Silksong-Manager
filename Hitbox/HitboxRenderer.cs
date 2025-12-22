using UnityEngine;
using System.Collections.Generic;
using GlobalEnums;

namespace SilksongManager.Hitbox
{
    public class HitboxRenderer : MonoBehaviour
    {
        private List<Collider2D> _visibleColliders = new List<Collider2D>();
        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (!HitboxConfig.ShowHitboxes) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            UpdateVisibleColliders();
        }

        private void UpdateVisibleColliders()
        {
            _visibleColliders.Clear();

            // Calculate camera bounds in world space
            float height = 2f * _cam.orthographicSize;
            float width = height * _cam.aspect;
            
            // Add a small buffer to ensure we catch edge items
            Vector2 camPos = _cam.transform.position;
            Vector2 size = new Vector2(width, height) * 1.1f;
            Vector2 p1 = camPos - size / 2;
            Vector2 p2 = camPos + size / 2;

            Collider2D[] hits = Physics2D.OverlapAreaAll(p1, p2);
            _visibleColliders.AddRange(hits);
        }

        private void OnGUI()
        {
            if (!HitboxConfig.ShowHitboxes) return;
            if (Event.current.type != EventType.Repaint) return;
            if (_cam == null) return;

            foreach (var col in _visibleColliders)
            {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;

                HitboxLayer layer = ClassifyCollider(col);
                if (ShouldShow(layer))
                {
                    DrawCollider(col, GetColor(layer));
                }
            }
        }

        private HitboxLayer ClassifyCollider(Collider2D col)
        {
            // Cache these lookups if optimization is needed, but layer checks are fast
            int layer = col.gameObject.layer;

            if (layer == (int)PhysLayers.PLAYER || layer == (int)PhysLayers.HERO_BOX) return HitboxLayer.Player;
            if (layer == (int)PhysLayers.ENEMIES) return HitboxLayer.Enemy;
            if (layer == (int)PhysLayers.HERO_ATTACK || layer == (int)PhysLayers.ENEMY_ATTACK || layer == (int)PhysLayers.PROJECTILES) return HitboxLayer.Attack;
            if (layer == (int)PhysLayers.TERRAIN || layer == (int)PhysLayers.SOFT_TERRAIN) return HitboxLayer.Terrain;
            if (layer == (int)PhysLayers.DAMAGE_ALL) return HitboxLayer.Hazard;
            if (layer == (int)PhysLayers.INTERACTIVE_OBJECT) return HitboxLayer.Interactive;
            if (layer == (int)PhysLayers.ACTIVE_REGION) return HitboxLayer.Trigger;

            if (col.isTrigger) return HitboxLayer.Trigger;
            
            // Fallbacks based on components
            // Using string checks to avoid hard dependnecy on game types if possible, but this is a mod so we can assume assembly presence
            // However, sticking to layers is faster. 
            
            return HitboxLayer.Terrain; 
        }

        private bool ShouldShow(HitboxLayer layer)
        {
            switch (layer)
            {
                case HitboxLayer.Player: return HitboxConfig.ShowPlayer;
                case HitboxLayer.Enemy: return HitboxConfig.ShowEnemy;
                case HitboxLayer.Attack: return HitboxConfig.ShowAttack;
                case HitboxLayer.Terrain: return HitboxConfig.ShowTerrain;
                case HitboxLayer.Trigger: return HitboxConfig.ShowTrigger;
                case HitboxLayer.Hazard: return HitboxConfig.ShowHazard;
                case HitboxLayer.Breakable: return HitboxConfig.ShowBreakable;
                case HitboxLayer.Interactive: return HitboxConfig.ShowInteractive;
                default: return false;
            }
        }

        private Color GetColor(HitboxLayer layer)
        {
            switch (layer)
            {
                case HitboxLayer.Player: return HitboxConfig.PlayerColor;
                case HitboxLayer.Enemy: return HitboxConfig.EnemyColor;
                case HitboxLayer.Attack: return HitboxConfig.AttackColor;
                case HitboxLayer.Terrain: return HitboxConfig.TerrainColor;
                case HitboxLayer.Trigger: return HitboxConfig.TriggerColor;
                case HitboxLayer.Hazard: return HitboxConfig.HazardColor;
                case HitboxLayer.Breakable: return HitboxConfig.BreakableColor;
                case HitboxLayer.Interactive: return HitboxConfig.InteractiveColor;
                default: return Color.white;
            }
        }

        private void DrawCollider(Collider2D col, Color color)
        {
            if (col is BoxCollider2D box)
            {
                DrawBoxCollider(box, color);
            }
            else if (col is CircleCollider2D circle)
            {
                DrawCircleCollider(circle, color);
            }
            else if (col is PolygonCollider2D poly)
            {
                DrawPolygonCollider(poly, color);
            }
            else if (col is EdgeCollider2D edge)
            {
                DrawEdgeCollider(edge, color);
            }
        }

        private Vector2 WorldToGUIPoint(Vector3 worldPos)
        {
            Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);
            return new Vector2(screenPos.x, Screen.height - screenPos.y);
        }

        private void DrawBoxCollider(BoxCollider2D box, Color color)
        {
            // BoxCollider2D is defined by offset and size in local space.
            // We need to transform the 4 corners to world space, then to GUI space.
            // Why 4 corners? Because the object might be rotated.
            
            Vector2 offset = box.offset;
            Vector2 size = box.size;

            Vector2 halfSize = size * 0.5f;
            Vector2 p1 = offset + new Vector2(-halfSize.x, -halfSize.y);
            Vector2 p2 = offset + new Vector2(halfSize.x, -halfSize.y);
            Vector2 p3 = offset + new Vector2(halfSize.x, halfSize.y);
            Vector2 p4 = offset + new Vector2(-halfSize.x, halfSize.y);

            Transform t = box.transform;
            Vector2 w1 = t.TransformPoint(p1);
            Vector2 w2 = t.TransformPoint(p2);
            Vector2 w3 = t.TransformPoint(p3);
            Vector2 w4 = t.TransformPoint(p4);

            Vector2 s1 = WorldToGUIPoint(w1);
            Vector2 s2 = WorldToGUIPoint(w2);
            Vector2 s3 = WorldToGUIPoint(w3);
            Vector2 s4 = WorldToGUIPoint(w4);

            Drawing.DrawLine(s1, s2, color, HitboxConfig.LineThickness);
            Drawing.DrawLine(s2, s3, color, HitboxConfig.LineThickness);
            Drawing.DrawLine(s3, s4, color, HitboxConfig.LineThickness);
            Drawing.DrawLine(s4, s1, color, HitboxConfig.LineThickness);
        }

        private void DrawCircleCollider(CircleCollider2D circle, Color color)
        {
            Vector2 centerStart = circle.offset;
            Transform t = circle.transform;
            Vector2 worldCenter = t.TransformPoint(centerStart);
            
            // Scale radius by max scale axis to handle non-uniform scale (approximate for ellipse if non-uniform)
            // But circle colliders in Unity 2D with non-uniform scale become ellipses. 
            // For simple viz, we can take one axis or try to draw ellipse.
            // Let's assume uniform or take X scale.
            float r = circle.radius * Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
            
            // Draw as circle in world logic, but need to project
            // Simpler: Project center, project a point at radius, use distance as screen radius?
            // Only works for uniform camera. Ortho camera is uniform.
            
            Vector2 screenCenter = WorldToGUIPoint(worldCenter);
            Vector3 worldRadiusPoint = worldCenter + new Vector2(r, 0);
            Vector2 screenRadiusPoint = WorldToGUIPoint(worldRadiusPoint);
            float screenRadius = Vector2.Distance(screenCenter, screenRadiusPoint);

            Drawing.DrawCircle(screenCenter, screenRadius, color, HitboxConfig.LineThickness);
            
            // Orientation line?
            // Drawing.DrawLine(screenCenter, ...);
        }

        private void DrawPolygonCollider(PolygonCollider2D poly, Color color)
        {
            Transform t = poly.transform;
            for (int i = 0; i < poly.pathCount; i++)
            {
                Vector2[] path = poly.GetPath(i);
                for (int j = 0; j < path.Length; j++)
                {
                    Vector2 pA = path[j];
                    Vector2 pB = path[(j + 1) % path.Length];
                    
                    Vector2 wA = t.TransformPoint(pA + poly.offset);
                    Vector2 wB = t.TransformPoint(pB + poly.offset);
                    
                    Drawing.DrawLine(WorldToGUIPoint(wA), WorldToGUIPoint(wB), color, HitboxConfig.LineThickness);
                }
            }
        }

        private void DrawEdgeCollider(EdgeCollider2D edge, Color color)
        {
            Transform t = edge.transform;
            Vector2[] points = edge.points;
            if (points == null || points.Length < 2) return;

            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 pA = points[i];
                Vector2 pB = points[i+1];
                
                Vector2 wA = t.TransformPoint(pA + edge.offset);
                Vector2 wB = t.TransformPoint(pB + edge.offset);
                
                Drawing.DrawLine(WorldToGUIPoint(wA), WorldToGUIPoint(wB), color, HitboxConfig.LineThickness);
            }
        }
    }
}
