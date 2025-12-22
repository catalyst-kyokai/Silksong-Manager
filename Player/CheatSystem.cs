using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;

namespace SilksongManager.Player
{
    /// <summary>
    /// Central cheat system managing all cheat states.
    /// Uses reflection to access game internals.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class CheatSystem
    {
        // Cheat states
        private static bool _infiniteJumps = false;
        private static bool _infiniteHealth = false;
        private static bool _infiniteSilk = false;
        private static bool _noclipEnabled = false;

        // Noclip state
        private static float _originalGravityScale = 1f;
        private static RigidbodyType2D _originalBodyType;
        private static bool _wasInvincible = false;
        private static Dictionary<Collider2D, bool> _colliderStates = new Dictionary<Collider2D, bool>();

        // Health/Silk tracking
        private static int _lastHealth = 0;
        private static int _lastSilk = 0;

        // Reflection cache
        private static FieldInfo _doubleJumpedField;
        private static bool _reflectionInitialized = false;

        // Config
        private static ConfigEntry<bool> _infiniteJumpsConfig;
        private static ConfigEntry<bool> _infiniteHealthConfig;
        private static ConfigEntry<bool> _infiniteSilkConfig;

        // Properties
        public static bool InfiniteJumps => _infiniteJumps;
        public static bool InfiniteHealth => _infiniteHealth;
        public static bool InfiniteSilk => _infiniteSilk;
        public static bool NoclipEnabled => _noclipEnabled;

        /// <summary>
        /// Initialize cheat system with config.
        /// </summary>
        public static void Initialize(ConfigFile config)
        {
            _infiniteJumpsConfig = config.Bind("Cheats", "InfiniteJumps", false, "Enable infinite jumps");
            _infiniteHealthConfig = config.Bind("Cheats", "InfiniteHealth", false, "Enable infinite health");
            _infiniteSilkConfig = config.Bind("Cheats", "InfiniteSilk", false, "Enable infinite silk");

            _infiniteJumps = _infiniteJumpsConfig.Value;
            _infiniteHealth = _infiniteHealthConfig.Value;
            _infiniteSilk = _infiniteSilkConfig.Value;

            InitializeReflection();
        }

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;

            try
            {
                _doubleJumpedField = typeof(HeroController).GetField(
                    "doubleJumped",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (_doubleJumpedField != null)
                {
                    Plugin.Log.LogInfo("CheatSystem: Found HeroController.doubleJumped field");
                }
                else
                {
                    Plugin.Log.LogWarning("CheatSystem: Could not find HeroController.doubleJumped field");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"CheatSystem reflection init failed: {e.Message}");
            }

            _reflectionInitialized = true;
        }

        /// <summary>
        /// Call every frame to process cheats.
        /// </summary>
        public static void Update()
        {
            var hero = Plugin.Hero;
            var pd = Plugin.PD;

            if (hero == null || pd == null) return;

            // Infinite Jumps: reset doubleJumped flag
            if (_infiniteJumps)
            {
                ProcessInfiniteJumps(hero);
            }

            // Infinite Health: restore health if decreased
            if (_infiniteHealth)
            {
                ProcessInfiniteHealth(pd);
            }
            else
            {
                _lastHealth = pd.health;
            }

            // Infinite Silk: restore silk if decreased
            if (_infiniteSilk)
            {
                ProcessInfiniteSilk(pd);
            }
            else
            {
                _lastSilk = pd.silk;
            }

            // Noclip movement
            if (_noclipEnabled)
            {
                ProcessNoclipMovement(hero);
            }
        }

        private static void ProcessInfiniteJumps(HeroController hero)
        {
            // Make game ALWAYS think player is on ground = infinite jumps anywhere
            // Set unconditionally every frame - no conditions
            hero.cState.onGround = true;

            // Also reset doubleJumped flag as backup
            if (_doubleJumpedField != null)
            {
                try
                {
                    _doubleJumpedField.SetValue(hero, false);
                }
                catch { }
            }
        }

        private static void ProcessInfiniteHealth(PlayerData pd)
        {
            if (pd.health < _lastHealth && pd.health > 0)
            {
                pd.health = _lastHealth;
            }
            else
            {
                _lastHealth = pd.health;
            }
        }

        private static void ProcessInfiniteSilk(PlayerData pd)
        {
            if (pd.silk < _lastSilk)
            {
                pd.silk = _lastSilk;
            }
            else
            {
                _lastSilk = pd.silk;
            }
        }

        private static void ProcessNoclipMovement(HeroController hero)
        {
            var rb = hero.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            float speed = 15f;
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            // Use shift for faster movement
            if (Input.GetKey(KeyCode.LeftShift))
                speed = 30f;

            rb.linearVelocity = new Vector2(h * speed, v * speed);
        }

        #region Toggle Methods

        public static void ToggleInfiniteJumps()
        {
            _infiniteJumps = !_infiniteJumps;
            if (_infiniteJumpsConfig != null)
                _infiniteJumpsConfig.Value = _infiniteJumps;

            Plugin.Log.LogInfo($"Infinite Jumps: {(_infiniteJumps ? "ON" : "OFF")}");
        }

        public static void SetInfiniteJumps(bool value)
        {
            _infiniteJumps = value;
            if (_infiniteJumpsConfig != null)
                _infiniteJumpsConfig.Value = value;
        }

        public static void ToggleInfiniteHealth()
        {
            _infiniteHealth = !_infiniteHealth;
            if (_infiniteHealthConfig != null)
                _infiniteHealthConfig.Value = _infiniteHealth;

            // Store current health when enabling
            var pd = Plugin.PD;
            if (_infiniteHealth && pd != null)
            {
                _lastHealth = pd.health;
            }

            Plugin.Log.LogInfo($"Infinite Health: {(_infiniteHealth ? "ON" : "OFF")}");
        }

        public static void SetInfiniteHealth(bool value)
        {
            _infiniteHealth = value;
            if (_infiniteHealthConfig != null)
                _infiniteHealthConfig.Value = value;

            if (value)
            {
                var pd = Plugin.PD;
                if (pd != null) _lastHealth = pd.health;
            }
        }

        public static void ToggleInfiniteSilk()
        {
            _infiniteSilk = !_infiniteSilk;
            if (_infiniteSilkConfig != null)
                _infiniteSilkConfig.Value = _infiniteSilk;

            // Store current silk when enabling
            var pd = Plugin.PD;
            if (_infiniteSilk && pd != null)
            {
                _lastSilk = pd.silk;
            }

            Plugin.Log.LogInfo($"Infinite Silk: {(_infiniteSilk ? "ON" : "OFF")}");
        }

        public static void SetInfiniteSilk(bool value)
        {
            _infiniteSilk = value;
            if (_infiniteSilkConfig != null)
                _infiniteSilkConfig.Value = value;

            if (value)
            {
                var pd = Plugin.PD;
                if (pd != null) _lastSilk = pd.silk;
            }
        }

        public static void ToggleNoclip()
        {
            var hero = Plugin.Hero;
            if (hero == null) return;

            _noclipEnabled = !_noclipEnabled;

            var rb = hero.GetComponent<Rigidbody2D>();
            var colliders = hero.GetComponentsInChildren<Collider2D>();
            var pd = Plugin.PD;

            if (_noclipEnabled)
            {
                // Save original state
                if (rb != null)
                {
                    _originalGravityScale = rb.gravityScale;
                    _originalBodyType = rb.bodyType;

                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.linearVelocity = Vector2.zero;
                }

                // Save and disable colliders
                _colliderStates.Clear();
                foreach (var col in colliders)
                {
                    _colliderStates[col] = col.enabled;
                    col.enabled = false;
                }

                // Make invincible
                if (pd != null)
                {
                    _wasInvincible = pd.isInvincible;
                    pd.isInvincible = true;
                }
            }
            else
            {
                // Restore original state
                if (rb != null)
                {
                    rb.bodyType = _originalBodyType;
                    rb.gravityScale = _originalGravityScale;
                }

                // Restore colliders to their ORIGINAL state
                foreach (var col in colliders)
                {
                    if (_colliderStates.TryGetValue(col, out bool wasEnabled))
                    {
                        col.enabled = wasEnabled;
                    }
                    else
                    {
                        // Default to enabled if we didn't track it
                        col.enabled = true;
                    }
                }
                _colliderStates.Clear();

                // Restore invincibility state
                if (pd != null)
                {
                    pd.isInvincible = _wasInvincible;
                }
            }

            Plugin.Log.LogInfo($"Noclip: {(_noclipEnabled ? "ON" : "OFF")}");
        }

        public static void SetNoclip(bool value)
        {
            if (_noclipEnabled != value)
            {
                ToggleNoclip();
            }
        }

        #endregion
    }
}

