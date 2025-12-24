using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Harmony patches for speed control.
    /// Uses universal approach: patch physics and animations for all game objects.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class SpeedControlPatches
    {
        #region Fields

        private static Harmony _harmony;
        private static Dictionary<int, float> _originalEnemySpeeds = new();

        // Reflection for tk2d
        private static System.Type _tk2dAnimatorType;
        private static PropertyInfo _clipFpsProperty;
        private static PropertyInfo _currentClipProperty;
        private static bool _reflectionInitialized = false;

        private static bool _debugLogging = false;

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

                // Universal enemy speed via HealthManager
                TryPatch(typeof(HealthManager), "OnEnable", nameof(HealthManager_OnEnable_Postfix), ref patchCount);

                // Rigidbody2D velocity scaling
                TryPatch(typeof(Rigidbody2D), "set_linearVelocity", nameof(Rigidbody2D_SetVelocity_Prefix), ref patchCount, isPrefix: true);

                Plugin.Log.LogInfo($"SpeedControlPatches: {patchCount} patches applied");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"SpeedControlPatches failed: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void TryPatch(System.Type targetType, string methodName, string patchName, ref int count, bool isPrefix = false)
        {
            try
            {
                var original = AccessTools.Method(targetType, methodName);
                if (original == null)
                {
                    if (_debugLogging) Plugin.Log.LogWarning($"SpeedControl: {targetType.Name}.{methodName} not found");
                    return;
                }

                var patch = typeof(SpeedControlPatches).GetMethod(patchName, BindingFlags.Public | BindingFlags.Static);
                if (patch == null)
                {
                    if (_debugLogging) Plugin.Log.LogWarning($"SpeedControl: Patch {patchName} not found");
                    return;
                }

                if (isPrefix)
                    _harmony.Patch(original, prefix: new HarmonyMethod(patch));
                else
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
                    _currentClipProperty = _tk2dAnimatorType.GetProperty("CurrentClip");
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"SpeedControl reflection failed: {e.Message}");
            }

            _reflectionInitialized = true;
        }

        public static void Remove()
        {
            _harmony?.UnpatchSelf();
            _originalEnemySpeeds.Clear();
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
            if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: Applied attack speed to Downspike");
        }

        #endregion

        #region Enemy Patches

        public static void HealthManager_OnEnable_Postfix(HealthManager __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            // Store object ID for tracking
            int id = __instance.gameObject.GetInstanceID();
            if (!_originalEnemySpeeds.ContainsKey(id))
            {
                _originalEnemySpeeds[id] = 1f; // Mark as tracked
            }

            // Apply enemy animation speed
            float animMult = SpeedControlConfig.EffectiveEnemyAttack;
            if (!Mathf.Approximately(animMult, 1f))
            {
                ApplyTk2dSpeedMultiplierToChildren(__instance.gameObject, animMult);
            }
        }

        /// <summary>
        /// Intercept all Rigidbody2D velocity sets and scale for enemies.
        /// </summary>
        public static void Rigidbody2D_SetVelocity_Prefix(Rigidbody2D __instance, ref Vector2 value)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectiveEnemyMovement;
            if (Mathf.Approximately(mult, 1f)) return;

            // Check if this belongs to an enemy (has HealthManager)
            var hm = __instance.GetComponentInParent<HealthManager>();
            if (hm == null) return;

            // Don't modify hero
            var hero = HeroController.instance;
            if (hero != null && __instance.gameObject == hero.gameObject) return;

            // Scale velocity
            value = new Vector2(value.x * mult, value.y * mult);
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

        private static void ApplyTk2dSpeedMultiplierToChildren(GameObject go, float mult)
        {
            if (_tk2dAnimatorType == null || _clipFpsProperty == null) return;

            try
            {
                var animators = go.GetComponentsInChildren(_tk2dAnimatorType, true);
                foreach (var anim in animators)
                {
                    if (anim == null) continue;
                    try
                    {
                        float currentFps = (float)_clipFpsProperty.GetValue(anim);
                        _clipFpsProperty.SetValue(anim, currentFps * mult);
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
            _originalEnemySpeeds.Clear();
        }

        #endregion
    }
}
