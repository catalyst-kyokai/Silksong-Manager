using UnityEngine;

namespace SilksongManager.Hitbox
{
    public enum HitboxLayer
    {
        Player,
        Enemy,
        Attack,
        Terrain,
        Trigger,
        Hazard,
        Breakable,
        Interactive
    }

    public static class HitboxConfig
    {
        // Global Toggle
        public static bool ShowHitboxes = false;

        // Layer Toggles
        public static bool ShowPlayer = true;
        public static bool ShowEnemy = true;
        public static bool ShowAttack = true;
        public static bool ShowTerrain = true;
        public static bool ShowTrigger = false;
        public static bool ShowHazard = true;
        public static bool ShowBreakable = true;
        public static bool ShowInteractive = true;

        // Colors
        public static Color PlayerColor = Color.green;
        public static Color EnemyColor = Color.red;
        public static Color AttackColor = Color.yellow;
        public static Color TerrainColor = Color.grey;
        public static Color TriggerColor = Color.cyan;
        public static Color HazardColor = new Color(1f, 0.5f, 0f); // Orange
        public static Color BreakableColor = Color.magenta;
        public static Color InteractiveColor = Color.blue;
        
        // Appearance
        public static float LineThickness = 2f;
        public static bool FillHitboxes = false;
        public static float FillAlpha = 0.2f;
    }
}
