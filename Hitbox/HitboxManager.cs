using UnityEngine;

namespace SilksongManager.Hitbox
{
    public static class HitboxManager
    {
        private static HitboxRenderer _renderer;
        
        public static void Initialize(GameObject host)
        {
             if (_renderer == null)
             {
                 _renderer = host.AddComponent<HitboxRenderer>();
                 Plugin.Log.LogInfo("Hitbox system initialized.");
             }
        }
        
        public static void ToggleHitboxes()
        {
            HitboxConfig.ShowHitboxes = !HitboxConfig.ShowHitboxes;
        }
    }
}
