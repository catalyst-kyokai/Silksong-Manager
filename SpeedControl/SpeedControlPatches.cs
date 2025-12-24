using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Harmony patches for speed control.
    /// Enemy Movement = animation speed only (no physics changes)
    /// Enemy Attack = animation speed + projectile velocity
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class SpeedControlPatches
    {
        #region Fields

        private static Harmony _harmony;

        // Reflection for tk2d
        private static System.Type _tk2dAnimatorType;
        private static PropertyInfo _clipFpsProperty;
        private static PropertyInfo _defaultFpsProperty;
        private static bool _reflectionInitialized = false;

        // Track original enemy bullet speeds
        private static Dictionary<int, float> _originalBulletSpeeds = new();

        #endregion

        #region Public Methods

        public static void Apply()
        {
            try
            {
                InitializeReflection();
                _harmony = new Harmony("com.catalyst.silksongmanager.speedcontrol");

                int patchCount = 0;

                // GameManager patches
                TryPatch(typeof(GameManager), "UnpauseGame", nameof(GameManager_UnpauseGame_Postfix), ref patchCount);

                // HeroController patches
                TryPatch(typeof(HeroController), "Start", nameof(HeroController_Start_Postfix), ref patchCount);
                TryPatch(typeof(HeroController), "TakeDamage", nameof(HeroController_TakeDamage_Postfix), ref patchCount);
                TryPatch(typeof(HeroController), "Respawn", nameof(HeroController_Respawn_Postfix), ref patchCount);

                // Player attack patches
                TryPatch(typeof(NailSlash), "PlaySlash", nameof(NailSlash_PlaySlash_Postfix), ref patchCount);
                TryPatch(typeof(Downspike), "StartSlash", nameof(Downspike_StartSlash_Postfix), ref patchCount);

                // Enemy animation speed (applies to all enemies via tk2d)
                TryPatch(typeof(HealthManager), "OnEnable", nameof(HealthManager_OnEnable_Postfix), ref patchCount);

                // Enemy projectile speed
                TryPatch(typeof(EnemyBullet), "OnEnable", nameof(EnemyBullet_OnEnable_Postfix), ref patchCount);

                Plugin.Log.LogInfo($"SpeedControlPatches: {patchCount} patches applied");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"SpeedControlPatches failed: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void TryPatch(System.Type targetType, string methodName, string patchName, ref int count)
        {
            try
            {
                var original = AccessTools.Method(targetType, methodName);
                if (original == null) return;

                var patch = typeof(SpeedControlPatches).GetMethod(patchName, BindingFlags.Public | BindingFlags.Static);
                if (patch == null) return;

                _harmony.Patch(original, postfix: new HarmonyMethod(patch));
                count++;
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"SpeedControl: Failed to patch {targetType.Name}.{methodName}: {e.Message}");
            }
        }

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;

            try
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    _tk2dAnimatorType = asm.GetType("tk2dSpriteAnimator");
                    if (_tk2dAnimatorType != null) break;
                }

                if (_tk2dAnimatorType != null)
                {
                    _clipFpsProperty = _tk2dAnimatorType.GetProperty("ClipFps");
                    _defaultFpsProperty = _tk2dAnimatorType.GetProperty("DefaultFps");
                }
            }
            catch { }

            _reflectionInitialized = true;
        }

        public static void Remove()
        {
            _harmony?.UnpatchSelf();
            _originalBulletSpeeds.Clear();
        }

        #endregion

        #region GameManager Patches

        public static void GameManager_UnpauseGame_Postfix()
        {
            if (!SpeedControlConfig.IsEnabled) return;

            if (SpeedControlConfig.GlobalSpeed != 1f)
            {
                Time.timeScale = SpeedControlConfig.GlobalSpeed;
            }
        }

        #endregion

        #region HeroController Patches

        public static void HeroController_Start_Postfix(HeroController __instance)
        {
            if (!SpeedControlConfig.OriginalsCaptured)
            {
                SpeedControlConfig.OriginalRunSpeed = __instance.RUN_SPEED;
                SpeedControlConfig.OriginalWalkSpeed = __instance.WALK_SPEED;
                SpeedControlConfig.OriginalsCaptured = true;
            }
            SpeedControlManager.ApplyPlayerSpeed();
        }

        public static void HeroController_TakeDamage_Postfix(HeroController __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;
            SpeedControlManager.ApplyPlayerSpeed();
        }

        public static void HeroController_Respawn_Postfix()
        {
            if (!SpeedControlConfig.IsEnabled) return;
            if (Plugin.Instance != null)
            {
                Plugin.Instance.StartCoroutine(ApplySpeedsNextFrame());
            }
        }

        #endregion

        #region Player Attack Patches

        public static void NailSlash_PlaySlash_Postfix(NailSlash __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectivePlayerAttack;
            if (Mathf.Approximately(mult, 1f)) return;

            ApplyTk2dSpeedMultiplier(__instance.gameObject, mult);
        }

        public static void Downspike_StartSlash_Postfix(Downspike __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectivePlayerAttack;
            if (Mathf.Approximately(mult, 1f)) return;

            ApplyTk2dSpeedMultiplier(__instance.gameObject, mult);
        }

        #endregion

        #region Enemy Animation Patches

        /// <summary>
        /// When enemy activates, apply animation speed multiplier.
        /// This affects both movement AND attack animations.
        /// Enemy Movement = how fast they animate while moving
        /// Enemy Attack = how fast attack animations play
        /// Combined multiplier is the effective animation speed.
        /// </summary>
        public static void HealthManager_OnEnable_Postfix(HealthManager __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            // Apply combined enemy animation speed (movement * attack for overall)
            // For now, use attack speed for all enemy animations
            float animMult = SpeedControlConfig.EffectiveEnemyAttack;

            // Movement affects how quickly they cycle through walk/fly anims
            float moveMult = SpeedControlConfig.EffectiveEnemyMovement;

            // Use the higher of the two (or combine them)
            float combinedMult = Mathf.Max(animMult, moveMult);

            if (Mathf.Approximately(combinedMult, 1f)) return;

            ApplyTk2dSpeedToEnemy(__instance.gameObject, combinedMult);
        }

        #endregion

        #region Enemy Projectile Patches

        /// <summary>
        /// When enemy bullet spawns, scale its velocity by attack speed.
        /// </summary>
        public static void EnemyBullet_OnEnable_Postfix(EnemyBullet __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectiveEnemyAttack;
            if (Mathf.Approximately(mult, 1f)) return;

            // Scale velocity on next frame (after initial velocity is set)
            if (Plugin.Instance != null)
            {
                Plugin.Instance.StartCoroutine(ScaleBulletVelocity(__instance, mult));
            }
        }

        private static System.Collections.IEnumerator ScaleBulletVelocity(EnemyBullet bullet, float mult)
        {
            yield return null; // Wait one frame for velocity to be set

            if (bullet == null) yield break;

            var rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                rb.linearVelocity *= mult;
            }
        }

        #endregion

        #region Helper Methods

        private static void ApplyTk2dSpeedMultiplier(GameObject go, float mult)
        {
            if (_tk2dAnimatorType == null || _clipFpsProperty == null) return;

            try
            {
                var animComponent = go.GetComponent(_tk2dAnimatorType);
                if (animComponent == null) return;

                float currentFps = (float)_clipFpsProperty.GetValue(animComponent);
                _clipFpsProperty.SetValue(animComponent, currentFps * mult);
            }
            catch { }
        }

        private static void ApplyTk2dSpeedToEnemy(GameObject go, float mult)
        {
            if (_tk2dAnimatorType == null || _clipFpsProperty == null) return;

            try
            {
                // Apply to all tk2d animators on this enemy and children
                var animators = go.GetComponentsInChildren(_tk2dAnimatorType, true);
                foreach (var anim in animators)
                {
                    if (anim == null) continue;
                    try
                    {
                        float currentFps = (float)_clipFpsProperty.GetValue(anim);
                        if (currentFps > 0)
                        {
                            _clipFpsProperty.SetValue(anim, currentFps * mult);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static System.Collections.IEnumerator ApplySpeedsNextFrame()
        {
            yield return null;
            SpeedControlManager.ApplyAllSpeeds();
        }

        public static void ResetWalkerSpeeds()
        {
            _originalBulletSpeeds.Clear();
        }

        #endregion
    }
}
