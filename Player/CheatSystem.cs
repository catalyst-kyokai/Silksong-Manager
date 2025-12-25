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
        /// <summary>Whether user explicitly enabled invincibility.</summary>
        private static bool _userInvincible = false;

        #endregion

        #region Noclip State

        /// <summary>Noclip position tracking (like debug mod).</summary>
        private static Vector3 _noclipPos;
        /// <summary>Original gravity scale before noclip was enabled.</summary>
        private static float _originalGravityScale = 1f;

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
        /// <summary>Config entry for user invincibility persistence.</summary>
        private static ConfigEntry<bool> _userInvincibleConfig;
        /// <summary>Config entry for noclip normal speed.</summary>
        private static ConfigEntry<float> _noclipSpeedConfig;
        /// <summary>Config entry for noclip boost speed.</summary>
        private static ConfigEntry<float> _noclipBoostSpeedConfig;

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
        /// <summary>Gets whether user explicitly enabled invincibility.</summary>
        public static bool UserInvincible => _userInvincible;

        /// <summary>Noclip normal movement speed.</summary>
        public static float NoclipSpeed
        {
            get => _noclipSpeedConfig?.Value ?? 15f;
            set { if (_noclipSpeedConfig != null) _noclipSpeedConfig.Value = value; }
        }

        /// <summary>Noclip boost movement speed (when holding boost key).</summary>
        public static float NoclipBoostSpeed
        {
            get => _noclipBoostSpeedConfig?.Value ?? 30f;
            set { if (_noclipBoostSpeedConfig != null) _noclipBoostSpeedConfig.Value = value; }
        }

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
            _userInvincibleConfig = config.Bind("Cheats", "UserInvincible", false, "User-enabled invincibility");
            _noclipSpeedConfig = config.Bind("Cheats", "NoclipSpeed", 15f, "Noclip normal movement speed");
            _noclipBoostSpeedConfig = config.Bind("Cheats", "NoclipBoostSpeed", 30f, "Noclip boost movement speed");

            _infiniteJumps = _infiniteJumpsConfig.Value;
            _infiniteHealth = _infiniteHealthConfig.Value;
            _infiniteSilk = _infiniteSilkConfig.Value;
            _userInvincible = _userInvincibleConfig.Value;

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

            // Force invincibility if user enabled it OR noclip is active
            if (_userInvincible || _noclipEnabled)
            {
                pd.isInvincible = true;
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
        /// Processes infinite silk by keeping silk at maximum.
        /// </summary>
        private static void ProcessInfiniteSilk(PlayerData pd)
        {
            // Only refill if below max
            if (pd.silk < pd.silkMax)
            {
                var hero = Plugin.Hero;
                if (hero != null)
                {
                    // Use AddSilk to properly update the HUD
                    int silkNeeded = pd.silkMax - pd.silk;
                    hero.AddSilk(silkNeeded, heroEffect: false);
                }
            }
            _lastSilk = pd.silk;
        }

        /// <summary>
        /// Processes noclip movement based on input.
        /// Uses transform.position directly like the working debug mod.
        /// </summary>
        private static void ProcessNoclipMovement(HeroController hero)
        {
            var rb = hero.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            // Get input handler
            var inputHandler = InputHandler.Instance;
            if (inputHandler == null || inputHandler.inputActions == null)
                return;

            // Calculate speed (base or boost)
            float baseSpeed = NoclipSpeed;
            if (Menu.Keybinds.ModKeybindManager.IsKeyHeld(Menu.Keybinds.ModAction.NoclipSpeedBoost))
                baseSpeed = NoclipBoostSpeed;

            float distance = baseSpeed * Time.deltaTime;

            // Calculate offset - SEPARATE if for each direction (not else if!)
            // This allows diagonal movement
            Vector3 offset = Vector3.zero;
            if (inputHandler.inputActions.Left.IsPressed)
                offset += Vector3.left * distance;
            if (inputHandler.inputActions.Right.IsPressed)
                offset += Vector3.right * distance;
            if (inputHandler.inputActions.Up.IsPressed)
                offset += Vector3.up * distance;
            if (inputHandler.inputActions.Down.IsPressed)
                offset += Vector3.down * distance;

            // Update noclip position
            _noclipPos += offset;

            // Check transition state - this is KEY for scene transitions!
            if (hero.transitionState == GlobalEnums.HeroTransitionState.WAITING_TO_TRANSITION)
            {
                // Not transitioning - apply position directly and freeze physics
                hero.transform.position = _noclipPos;
                rb.constraints = rb.constraints | RigidbodyConstraints2D.FreezePosition;
            }
            else
            {
                // During transition - sync position and allow physics for triggers
                _noclipPos = hero.transform.position;
                rb.constraints = rb.constraints & ~RigidbodyConstraints2D.FreezePosition;
            }
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

        public static void ToggleUserInvincible()
        {
            _userInvincible = !_userInvincible;
            if (_userInvincibleConfig != null)
                _userInvincibleConfig.Value = _userInvincible;

            var pd = Plugin.PD;
            if (pd != null)
            {
                pd.isInvincible = _userInvincible;
            }

            Plugin.Log.LogInfo($"User Invincible: {(_userInvincible ? "ON" : "OFF")}");
        }

        public static void SetUserInvincible(bool value)
        {
            _userInvincible = value;
            if (_userInvincibleConfig != null)
                _userInvincibleConfig.Value = value;

            var pd = Plugin.PD;
            if (pd != null)
            {
                pd.isInvincible = value;
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
                // Save current position for noclip tracking
                _noclipPos = hero.transform.position;

                // Save original gravity
                if (rb != null)
                {
                    _originalGravityScale = rb.gravityScale;
                }

                // Make invincible during noclip
                if (pd != null)
                {
                    pd.isInvincible = true;
                }
            }
            else
            {
                // Restore physics on disable
                if (rb != null)
                {
                    rb.gravityScale = _originalGravityScale;
                    // Remove freeze constraints
                    rb.constraints = rb.constraints & ~RigidbodyConstraints2D.FreezePosition;
                }

                // Restore invincibility state - ONLY if user didn't explicitly enable it
                if (pd != null && !_userInvincible)
                {
                    pd.isInvincible = false;
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

