using UnityEngine;

namespace SilksongManager.Player
{
    /// <summary>
    /// Actions related to the player/hero character.
    /// Provides methods for health, silk, position, and ability management.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class PlayerActions
    {
        #region State Fields

        /// <summary>Legacy field for infinite jumps toggle.</summary>
        private static bool _infiniteJumpsEnabled = false;
        /// <summary>Legacy field for noclip toggle.</summary>
        private static bool _noclipEnabled = false;
        /// <summary>Saved position for teleportation.</summary>
        private static Vector3 _savedPosition = Vector3.zero;

        #endregion

        #region Quick Actions

        /// <summary>
        /// Heal the player to full health.
        /// </summary>
        public static void QuickHeal()
        {
            var pd = Plugin.PD;
            var hero = Plugin.Hero;

            if (pd == null || hero == null)
            {
                Plugin.Log.LogWarning("Cannot heal: not in game.");
                return;
            }

            int healAmount = pd.maxHealth - pd.health;
            if (healAmount > 0)
            {
                hero.AddHealth(healAmount);
                Plugin.Log.LogInfo($"Healed for {healAmount} HP. Current: {pd.health}/{pd.maxHealth}");
            }
        }

        /// <summary>
        /// Refill silk to maximum.
        /// </summary>
        public static void QuickSilk()
        {
            var pd = Plugin.PD;
            var hero = Plugin.Hero;

            if (pd == null || hero == null)
            {
                Plugin.Log.LogWarning("Cannot refill silk: not in game.");
                return;
            }

            int silkAmount = pd.silkMax - pd.silk;
            if (silkAmount > 0)
            {
                hero.AddSilk(silkAmount, heroEffect: false);
                Plugin.Log.LogInfo($"Refilled {silkAmount} silk. Current: {pd.silk}/{pd.silkMax}");
            }
        }

        /// <summary>
        /// Toggle player invincibility.
        /// </summary>
        public static void ToggleInvincibility()
        {
            var pd = Plugin.PD;

            if (pd == null)
            {
                Plugin.Log.LogWarning("Cannot toggle invincibility: not in game.");
                return;
            }

            pd.isInvincible = !pd.isInvincible;
            Plugin.Log.LogInfo($"Invincibility: {(pd.isInvincible ? "ON" : "OFF")}");
        }

        /// <summary>
        /// Set player health to specific value.
        /// </summary>
        public static void SetHealth(int health)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            int diff = health - pd.health;
            if (diff > 0)
            {
                Plugin.Hero?.AddHealth(diff);
            }
            else if (diff < 0)
            {
                pd.health = Mathf.Max(1, health);
            }
            Plugin.Log.LogInfo($"Set health to {pd.health}");
        }

        #endregion

        #region Max Stats

        /// <summary>
        /// Set player max health.
        /// </summary>
        public static void SetMaxHealth(int maxHealth)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            pd.maxHealth = maxHealth;
            Plugin.Log.LogInfo($"Set max health to {pd.maxHealth}");
        }

        /// <summary>
        /// Set player silk to specific value.
        /// </summary>
        public static void SetSilk(int silk)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            pd.silk = Mathf.Clamp(silk, 0, pd.silkMax);
            Plugin.Log.LogInfo($"Set silk to {pd.silk}");
        }

        /// <summary>
        /// Toggle infinite jumps.
        /// </summary>
        public static void ToggleInfiniteJumps()
        {
            _infiniteJumpsEnabled = !_infiniteJumpsEnabled;
            Plugin.Log.LogInfo($"Infinite Jumps: {(_infiniteJumpsEnabled ? "ON" : "OFF")}");
        }

        /// <summary>
        /// Gets whether infinite jumps is enabled (legacy).
        /// </summary>
        public static bool IsInfiniteJumpsEnabled => _infiniteJumpsEnabled;

        #endregion

        #region Noclip

        /// <summary>
        /// Toggle noclip mode.
        /// </summary>
        public static void ToggleNoclip()
        {
            var hero = Plugin.Hero;
            if (hero == null) return;

            _noclipEnabled = !_noclipEnabled;

            var rb = hero.GetComponent<Rigidbody2D>();
            var colliders = hero.GetComponentsInChildren<Collider2D>();

            if (_noclipEnabled)
            {
                if (rb != null)
                {
                    rb.gravityScale = 0f;
                    rb.linearVelocity = Vector2.zero;
                }

                foreach (var col in colliders)
                {
                    col.enabled = false;
                }
            }
            else
            {
                if (rb != null)
                {
                    rb.gravityScale = 1f;
                }

                foreach (var col in colliders)
                {
                    col.enabled = true;
                }
            }

            Plugin.Log.LogInfo($"Noclip: {(_noclipEnabled ? "ON" : "OFF")}");
        }

        /// <summary>
        /// Check if noclip is enabled.
        /// </summary>
        public static bool IsNoclipEnabled => _noclipEnabled;

        /// <summary>
        /// Process noclip movement (call in Update).
        /// </summary>
        public static void ProcessNoclipMovement(float speed = 15f)
        {
            if (!_noclipEnabled) return;

            var hero = Plugin.Hero;
            if (hero == null) return;

            var rb = hero.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            rb.linearVelocity = new Vector2(h * speed, v * speed);
        }

        #endregion

        #region Teleportation

        /// <summary>
        /// Teleport player to a specific position.
        /// </summary>
        public static void TeleportTo(Vector3 position)
        {
            var hero = Plugin.Hero;
            if (hero == null)
            {
                Plugin.Log.LogWarning("Cannot teleport: not in game.");
                return;
            }

            hero.transform.position = position;
            Plugin.Log.LogInfo($"Teleported to {position}");
        }

        /// <summary>
        /// Kill the player.
        /// </summary>
        public static void KillPlayer()
        {
            var hero = Plugin.Hero;
            if (hero == null) return;

            // Trigger death
            var pd = Plugin.PD;
            if (pd != null)
            {
                pd.health = 0;
            }

            Plugin.Log.LogInfo("Killed player.");
        }

        /// <summary>
        /// Respawn the player.
        /// </summary>
        public static void RespawnPlayer()
        {
            var gm = Plugin.GM;
            if (gm == null) return;

            gm.ReadyForRespawn(false);
            Plugin.Log.LogInfo("Respawning player.");
        }

        #endregion

        #region State Query

        /// <summary>
        /// Get current player state info.
        /// </summary>
        public static PlayerStateInfo GetStateInfo()
        {
            var hero = Plugin.Hero;
            var pd = Plugin.PD;

            if (hero == null || pd == null)
            {
                return new PlayerStateInfo();
            }

            return new PlayerStateInfo
            {
                Position = hero.transform.position,
                Health = pd.health,
                MaxHealth = pd.maxHealth,
                Silk = pd.silk,
                MaxSilk = pd.silkMax,
                IsInvincible = pd.isInvincible,
                FacingRight = hero.cState.facingRight,
                OnGround = hero.cState.onGround,
                Jumping = hero.cState.jumping,
                Dashing = hero.cState.dashing,
                Attacking = hero.cState.attacking,
                Dead = hero.cState.dead,
                SceneName = Plugin.GM?.sceneName ?? "Unknown"
            };
        }

        /// <summary>
        /// Unlock all abilities.
        /// </summary>
        public static void UnlockAllAbilities()
        {
            ToolItemManager.UnlockAllTools();
            ToolItemManager.UnlockAllCrests();
            Plugin.Log.LogInfo("Unlocked all tools and crests.");
        }

        #endregion
    }

    /// <summary>
    /// Player state information.
    /// </summary>
    public struct PlayerStateInfo
    {
        public Vector3 Position;
        public int Health;
        public int MaxHealth;
        public int Silk;
        public int MaxSilk;
        public bool IsInvincible;
        public bool FacingRight;
        public bool OnGround;
        public bool Jumping;
        public bool Dashing;
        public bool Attacking;
        public bool Dead;
        public string SceneName;
    }
}
