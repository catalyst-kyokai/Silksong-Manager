using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Main manager for speed control system.
    /// Provides API for controlling game, player, enemy, and environment speeds.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class SpeedControlManager
    {
        #region Fields

        /// <summary>Tracks if system is initialized.</summary>
        private static bool _initialized = false;

        /// <summary>Cached hero reference.</summary>
        private static HeroController _cachedHero;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the speed control system.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            SpeedControlPatches.Apply();
            SceneManager.sceneLoaded += OnSceneLoaded;

            _initialized = true;
            Plugin.Log.LogInfo("SpeedControl system initialized");
        }

        /// <summary>
        /// Cleanup on shutdown.
        /// </summary>
        public static void Shutdown()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SpeedControlPatches.Remove();
            SpeedControlConfig.ResetAll();
            _initialized = false;
        }

        #endregion

        #region Global Speed

        /// <summary>
        /// Set global game speed (Time.timeScale).
        /// </summary>
        /// <param name="speed">Speed multiplier (0.1 - 10.0).</param>
        public static void SetGlobalSpeed(float speed)
        {
            SpeedControlConfig.GlobalSpeed = Mathf.Clamp(speed, 0.1f, 10f);
            ApplyGlobalSpeed();
            Plugin.Log.LogInfo($"Global speed set to {SpeedControlConfig.GlobalSpeed:F2}x");
        }

        /// <summary>
        /// Apply the stored global speed to Time.timeScale.
        /// </summary>
        public static void ApplyGlobalSpeed()
        {
            if (!SpeedControlConfig.IsEnabled) return;

            // Only apply if game is not paused
            var gm = Plugin.GM;
            if (gm != null && gm.IsGamePaused()) return;

            Time.timeScale = SpeedControlConfig.GlobalSpeed;
        }

        #endregion

        #region Player Speed

        /// <summary>
        /// Set player movement speed multiplier.
        /// </summary>
        public static void SetPlayerMovementSpeed(float speed)
        {
            SpeedControlConfig.PlayerMovementSpeed = Mathf.Clamp(speed, 0.1f, 10f);
            ApplyPlayerSpeed();
            Plugin.Log.LogInfo($"Player movement speed set to {speed:F2}x");
        }

        /// <summary>
        /// Set player attack speed multiplier.
        /// </summary>
        public static void SetPlayerAttackSpeed(float speed)
        {
            SpeedControlConfig.PlayerAttackSpeed = Mathf.Clamp(speed, 0.1f, 10f);
            ApplyPlayerAttackSpeed();
            Plugin.Log.LogInfo($"Player attack speed set to {speed:F2}x");
        }

        /// <summary>
        /// Set combined player speed (movement + attacks).
        /// </summary>
        public static void SetPlayerAllSpeed(float speed)
        {
            SpeedControlConfig.PlayerAllSpeed = Mathf.Clamp(speed, 0.1f, 10f);
            ApplyPlayerSpeed();
            ApplyPlayerAttackSpeed();
            Plugin.Log.LogInfo($"Player all speed set to {speed:F2}x");
        }

        /// <summary>
        /// Apply player movement speed to HeroController.
        /// </summary>
        public static void ApplyPlayerSpeed()
        {
            if (!SpeedControlConfig.IsEnabled) return;

            var hero = Plugin.Hero;
            if (hero == null) return;

            // Capture original values if not yet done
            if (!SpeedControlConfig.OriginalsCaptured)
            {
                SpeedControlConfig.OriginalRunSpeed = hero.RUN_SPEED;
                SpeedControlConfig.OriginalWalkSpeed = hero.WALK_SPEED;
                SpeedControlConfig.OriginalsCaptured = true;
            }

            // Apply multiplier
            float mult = SpeedControlConfig.EffectivePlayerMovement;
            hero.RUN_SPEED = SpeedControlConfig.OriginalRunSpeed * mult;
            hero.WALK_SPEED = SpeedControlConfig.OriginalWalkSpeed * mult;
        }

        /// <summary>
        /// Apply player attack speed.
        /// Attack speed is handled via Harmony patches on attack methods.
        /// </summary>
        public static void ApplyPlayerAttackSpeed()
        {
            // Attack speed is primarily controlled via patches
            // This method can be used to update animator speed if needed
        }

        #endregion

        #region Enemy Speed

        /// <summary>
        /// Set enemy movement speed multiplier.
        /// </summary>
        public static void SetEnemyMovementSpeed(float speed)
        {
            SpeedControlConfig.EnemyMovementSpeed = Mathf.Clamp(speed, 0.1f, 10f);
            ApplyEnemySpeed();
            Plugin.Log.LogInfo($"Enemy movement speed set to {speed:F2}x");
        }

        /// <summary>
        /// Set enemy attack speed multiplier.
        /// </summary>
        public static void SetEnemyAttackSpeed(float speed)
        {
            SpeedControlConfig.EnemyAttackSpeed = Mathf.Clamp(speed, 0.1f, 10f);
            ApplyEnemyAnimatorSpeed();
            Plugin.Log.LogInfo($"Enemy attack speed set to {speed:F2}x");
        }

        /// <summary>
        /// Set combined enemy speed (movement + attacks).
        /// </summary>
        public static void SetEnemyAllSpeed(float speed)
        {
            SpeedControlConfig.EnemyAllSpeed = Mathf.Clamp(speed, 0.1f, 10f);
            ApplyEnemySpeed();
            ApplyEnemyAnimatorSpeed();
            Plugin.Log.LogInfo($"Enemy all speed set to {speed:F2}x");
        }

        /// <summary>
        /// Apply enemy movement speed modifications.
        /// </summary>
        public static void ApplyEnemySpeed()
        {
            // Enemy movement is handled via velocity scaling in patches
            // This ensures continuous application even for newly spawned enemies
        }

        /// <summary>
        /// Apply enemy animator speed for attacks.
        /// </summary>
        public static void ApplyEnemyAnimatorSpeed()
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectiveEnemyAttack;

            foreach (var hm in HealthManager.EnumerateActiveEnemies())
            {
                if (hm == null || hm.gameObject == null) continue;

                var animators = hm.GetComponentsInChildren<Animator>();
                foreach (var anim in animators)
                {
                    if (anim != null)
                    {
                        anim.speed = mult;
                    }
                }
            }
        }

        #endregion

        #region Environment Speed

        /// <summary>
        /// Set environment speed (particles, platforms, effects).
        /// </summary>
        public static void SetEnvironmentSpeed(float speed)
        {
            SpeedControlConfig.EnvironmentSpeed = Mathf.Clamp(speed, 0.1f, 10f);
            ApplyEnvironmentSpeed();
            Plugin.Log.LogInfo($"Environment speed set to {speed:F2}x");
        }

        /// <summary>
        /// Apply environment speed to animations and other elements.
        /// </summary>
        public static void ApplyEnvironmentSpeed()
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EnvironmentSpeed;

            // Apply to animators not on hero/enemies
            var animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (var anim in animators)
            {
                if (anim == null) continue;
                if (IsHeroOrEnemy(anim.gameObject)) continue;

                anim.speed = mult;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Reset all speeds to default (1.0x).
        /// </summary>
        public static void ResetAll()
        {
            SpeedControlConfig.ResetAll();

            // Restore originals
            var hero = Plugin.Hero;
            if (hero != null && SpeedControlConfig.OriginalsCaptured)
            {
                hero.RUN_SPEED = SpeedControlConfig.OriginalRunSpeed;
                hero.WALK_SPEED = SpeedControlConfig.OriginalWalkSpeed;
            }

            Time.timeScale = 1f;

            // Reset Walker speeds
            SpeedControlPatches.ResetWalkerSpeeds();

            Plugin.Log.LogInfo("All speeds reset to 1.0x");
        }

        /// <summary>
        /// Apply all current speed settings.
        /// Called after scene load, damage, etc.
        /// </summary>
        public static void ApplyAllSpeeds()
        {
            if (!SpeedControlConfig.IsEnabled) return;

            ApplyGlobalSpeed();
            ApplyPlayerSpeed();
            ApplyEnemyAnimatorSpeed();
            ApplyEnvironmentSpeed();
        }

        /// <summary>
        /// Check if a GameObject belongs to hero or an enemy.
        /// </summary>
        private static bool IsHeroOrEnemy(GameObject go)
        {
            if (go == null) return false;

            // Check for hero
            var hero = Plugin.Hero;
            if (hero != null && (go == hero.gameObject || go.transform.IsChildOf(hero.transform)))
                return true;

            // Check for enemy (HealthManager)
            var hm = go.GetComponentInParent<HealthManager>();
            if (hm != null) return true;

            return false;
        }

        /// <summary>
        /// Handle scene load - reapply all speeds.
        /// </summary>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Reset originals capture flag for new scene
            SpeedControlConfig.OriginalsCaptured = false;

            // Delay application to allow scene to initialize
            if (Plugin.Instance != null)
            {
                Plugin.Instance.StartCoroutine(ApplySpeedsDelayed());
            }
        }

        private static System.Collections.IEnumerator ApplySpeedsDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            ApplyAllSpeeds();
        }

        #endregion

        #region Update (Called from Plugin)

        /// <summary>
        /// Per-frame update to maintain enemy speed modifications.
        /// Should be called from Plugin.Update() or LateUpdate().
        /// </summary>
        public static void Update()
        {
            if (!SpeedControlConfig.IsEnabled) return;
            if (!SpeedControlConfig.IsAnyModified()) return;

            // Continuously apply enemy movement speed via velocity scaling
            float moveMult = SpeedControlConfig.EffectiveEnemyMovement;
            if (Mathf.Approximately(moveMult, 1f)) return;

            // This is handled in patches for better performance
        }

        #endregion
    }
}
