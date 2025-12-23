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
        #region Cheat State Fields

        /// <summary>Whether infinite jumps cheat is enabled.</summary>
        private static bool _infiniteJumps = false;
        /// <summary>Whether infinite health cheat is enabled.</summary>
        private static bool _infiniteHealth = false;
        /// <summary>Whether infinite silk cheat is enabled.</summary>
        private static bool _infiniteSilk = false;
        /// <summary>Whether noclip mode is enabled.</summary>
        private static bool _noclipEnabled = false;

        #endregion

        #region Noclip State

        /// <summary>Original gravity scale before noclip was enabled.</summary>
        private static float _originalGravityScale = 1f;
        /// <summary>Original body type before noclip was enabled.</summary>
        private static RigidbodyType2D _originalBodyType;
        /// <summary>Whether player was invincible before noclip.</summary>
        private static bool _wasInvincible = false;
        /// <summary>Collider enabled states before noclip.</summary>
        private static Dictionary<Collider2D, bool> _colliderStates = new Dictionary<Collider2D, bool>();

        #endregion

        #region Tracking Fields

        /// <summary>Last recorded health value for infinite health tracking.</summary>
        private static int _lastHealth = 0;
        /// <summary>Last recorded silk value for infinite silk tracking.</summary>
        private static int _lastSilk = 0;

        #endregion

        #region Reflection Cache

        /// <summary>Cached field info for HeroController.doubleJumped.</summary>
        private static FieldInfo _doubleJumpedField;
        /// <summary>Whether reflection has been initialized.</summary>
        private static bool _reflectionInitialized = false;

        #endregion

        #region Configuration Entries

        /// <summary>Config entry for infinite jumps persistence.</summary>
        private static ConfigEntry<bool> _infiniteJumpsConfig;
        /// <summary>Config entry for infinite health persistence.</summary>
        private static ConfigEntry<bool> _infiniteHealthConfig;
        /// <summary>Config entry for infinite silk persistence.</summary>
        private static ConfigEntry<bool> _infiniteSilkConfig;

        #endregion

        #region Public Properties

        /// <summary>Gets whether infinite jumps is currently enabled.</summary>
        public static bool InfiniteJumps => _infiniteJumps;
        /// <summary>Gets whether infinite health is currently enabled.</summary>
        public static bool InfiniteHealth => _infiniteHealth;
        /// <summary>Gets whether infinite silk is currently enabled.</summary>
        public static bool InfiniteSilk => _infiniteSilk;
        /// <summary>Gets whether noclip mode is currently enabled.</summary>
        public static bool NoclipEnabled => _noclipEnabled;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the cheat system with config file binding.
        /// </summary>
        /// <param name="config">BepInEx configuration file.</param>
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

        #endregion

        #region Update Processing

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

        #endregion

        #region Cheat Processing Methods

        /// <summary>
        /// Processes infinite jumps by keeping player grounded.
        /// </summary>
        private static void ProcessInfiniteJumps(HeroController hero)
        {
            hero.cState.onGround = true;

            if (_doubleJumpedField != null)
            {
                try
                {
                    _doubleJumpedField.SetValue(hero, false);
                }
                catch { }
            }
        }

        /// <summary>
        /// Processes infinite health by restoring health when decreased.
        /// </summary>
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

        /// <summary>
        /// Processes infinite silk by restoring silk when decreased.
        /// </summary>
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

        /// <summary>
        /// Processes noclip movement based on input.
        /// </summary>
        private static void ProcessNoclipMovement(HeroController hero)
        {
            var rb = hero.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            float speed = 15f;
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            if (Input.GetKey(KeyCode.LeftShift))
                speed = 30f;

            rb.linearVelocity = new Vector2(h * speed, v * speed);
        }

        #endregion

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

