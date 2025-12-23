using UnityEngine;

namespace SilksongManager.Hitbox
{
    /// <summary>
    /// Central manager for the hitbox visualization system.
    /// Handles initialization and provides global toggle functionality.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class HitboxManager
    {
        /// <summary>
        /// Reference to the HitboxRenderer component attached to the plugin GameObject.
        /// </summary>
        private static HitboxRenderer _renderer;

        /// <summary>
        /// Initializes the hitbox visualization system by creating a HitboxRenderer component.
        /// </summary>
        /// <param name="host">The GameObject to attach the HitboxRenderer component to (typically the Plugin GameObject).</param>
        public static void Initialize(GameObject host)
        {
            if (_renderer == null)
            {
                _renderer = host.AddComponent<HitboxRenderer>();
                Plugin.Log.LogInfo("Hitbox system initialized.");
            }
        }

        /// <summary>
        /// Toggles the global hitbox visualization on or off.
        /// </summary>
        public static void ToggleHitboxes()
        {
            HitboxConfig.ShowHitboxes = !HitboxConfig.ShowHitboxes;
        }
    }
}
