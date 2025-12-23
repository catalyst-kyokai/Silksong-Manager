using UnityEngine;

namespace SilksongManager.Hitbox
{
    /// <summary>
    /// Defines the different categories of hitboxes that can be visualized.
    /// Each layer can be toggled independently for clearer debugging.
    /// </summary>
    public enum HitboxLayer
    {
        /// <summary>Player character hitboxes (hurt boxes and collision).</summary>
        Player,
        /// <summary>Enemy character hitboxes.</summary>
        Enemy,
        /// <summary>Attack hitboxes from player, enemies, or projectiles.</summary>
        Attack,
        /// <summary>Terrain and environmental collision.</summary>
        Terrain,
        /// <summary>Trigger zones and event regions.</summary>
        Trigger,
        /// <summary>Hazard zones that deal damage (spikes, acid, etc.).</summary>
        Hazard,
        /// <summary>Breakable objects and destructibles.</summary>
        Breakable,
        /// <summary>Interactive objects (chests, levers, NPCs).</summary>
        Interactive
    }

    /// <summary>
    /// Static configuration for the hitbox visualization system.
    /// Controls which hitbox layers are visible and their visual appearance.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class HitboxConfig
    {
        #region Global Toggle

        /// <summary>Master toggle for hitbox visualization. When false, no hitboxes are drawn.</summary>
        public static bool ShowHitboxes = false;

        #endregion

        #region Layer Toggles

        /// <summary>Show player character hitboxes.</summary>
        public static bool ShowPlayer = true;
        /// <summary>Show enemy character hitboxes.</summary>
        public static bool ShowEnemy = true;
        /// <summary>Show attack hitboxes (player, enemy, projectiles).</summary>
        public static bool ShowAttack = true;
        /// <summary>Show terrain collision hitboxes.</summary>
        public static bool ShowTerrain = true;
        /// <summary>Show trigger zone hitboxes.</summary>
        public static bool ShowTrigger = false;
        /// <summary>Show hazard zone hitboxes.</summary>
        public static bool ShowHazard = true;
        /// <summary>Show breakable object hitboxes.</summary>
        public static bool ShowBreakable = true;
        /// <summary>Show interactive object hitboxes.</summary>
        public static bool ShowInteractive = true;

        #endregion

        #region Colors

        /// <summary>Color for player hitboxes (default: green).</summary>
        public static Color PlayerColor = Color.green;
        /// <summary>Color for enemy hitboxes (default: red).</summary>
        public static Color EnemyColor = Color.red;
        /// <summary>Color for attack hitboxes (default: yellow).</summary>
        public static Color AttackColor = Color.yellow;
        /// <summary>Color for terrain hitboxes (default: grey).</summary>
        public static Color TerrainColor = Color.grey;
        /// <summary>Color for trigger zone hitboxes (default: cyan).</summary>
        public static Color TriggerColor = Color.cyan;
        /// <summary>Color for hazard zone hitboxes (default: orange).</summary>
        public static Color HazardColor = new Color(1f, 0.5f, 0f);
        /// <summary>Color for breakable object hitboxes (default: magenta).</summary>
        public static Color BreakableColor = Color.magenta;
        /// <summary>Color for interactive object hitboxes (default: blue).</summary>
        public static Color InteractiveColor = Color.blue;

        #endregion

        #region Appearance

        /// <summary>Thickness of hitbox outline lines in pixels.</summary>
        public static float LineThickness = 2f;
        /// <summary>Whether to fill hitboxes with semi-transparent color.</summary>
        public static bool FillHitboxes = false;
        /// <summary>Alpha value for hitbox fill (0-1). Only used when FillHitboxes is true.</summary>
        public static float FillAlpha = 0.2f;

        #endregion
    }
}
