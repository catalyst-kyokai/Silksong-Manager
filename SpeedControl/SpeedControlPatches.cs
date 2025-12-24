using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Harmony patches for speed control.
    /// Handles player attacks via NailSlash and enemy movement via Walker.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class SpeedControlPatches
    {
        #region Fields

        /// <summary>Harmony instance for patching.</summary>
        private static Harmony _harmony;

        /// <summary>Tracks original Walker speeds for restoration.</summary>
        private static Dictionary<Walker, (float left, float right)> _originalWalkerSpeeds = new();

        /// <summary>Cached reflection for tk2dSpriteAnimator.</summary>
        private static System.Type _tk2dAnimatorType;
        private static PropertyInfo _clipFpsProperty;
        private static PropertyInfo _currentClipProperty;
        private static bool _reflectionInitialized = false;

        #endregion

        #region Public Methods

        /// <summary>
        /// Apply all speed control patches.
        /// </summary>
        public static void Apply()
        {
            try
            {
                InitializeReflection();
                _harmony = new Harmony("com.catalyst.silksongmanager.speedcontrol");
                _harmony.PatchAll(typeof(SpeedControlPatches));
                Plugin.Log.LogInfo("SpeedControlPatches applied successfully");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Failed to apply SpeedControlPatches: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;

            try
            {
                // Find tk2dSpriteAnimator type
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    _tk2dAnimatorType = asm.GetType("tk2dSpriteAnimator");
                    if (_tk2dAnimatorType != null) break;
                }

                if (_tk2dAnimatorType != null)
                {
                    _clipFpsProperty = _tk2dAnimatorType.GetProperty("ClipFps");
                    _currentClipProperty = _tk2dAnimatorType.GetProperty("CurrentClip");
                    Plugin.Log.LogInfo($"tk2dSpriteAnimator reflection initialized. ClipFps: {_clipFpsProperty != null}, CurrentClip: {_currentClipProperty != null}");
                }
                else
                {
                    Plugin.Log.LogWarning("tk2dSpriteAnimator type not found");
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Reflection init failed: {e.Message}");
            }

            _reflectionInitialized = true;
        }

        /// <summary>
        /// Remove all patches.
        /// </summary>
        public static void Remove()
        {
            _harmony?.UnpatchSelf();
            _originalWalkerSpeeds.Clear();
        }

        #endregion

        #region GameManager Patches

        /// <summary>
        /// After game unpauses, restore our custom time scale.
        /// </summary>
        [HarmonyPatch(typeof(GameManager), "UnpauseGame")]
        [HarmonyPostfix]
        public static void GameManager_UnpauseGame_Postfix()
        {
            if (!SpeedControlConfig.IsEnabled) return;

            if (SpeedControlConfig.GlobalSpeed != 1f)
            {
                Time.timeScale = SpeedControlConfig.GlobalSpeed;
            }
        }

        /// <summary>
        /// After game sets time scale, check if we need to override it.
        /// </summary>
        [HarmonyPatch(typeof(GameManager), "SetTimeScale")]
        [HarmonyPostfix]
        public static void GameManager_SetTimeScale_Postfix(float newTimeScale)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            if (Mathf.Approximately(newTimeScale, 1f) && SpeedControlConfig.GlobalSpeed != 1f)
            {
                Time.timeScale = SpeedControlConfig.GlobalSpeed;
            }
        }

        #endregion

        #region HeroController Patches

        /// <summary>
        /// Capture original speed values when HeroController starts.
        /// </summary>
        [HarmonyPatch(typeof(HeroController), "Start")]
        [HarmonyPostfix]
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

        /// <summary>
        /// After hero takes damage, reapply player speeds.
        /// </summary>
        [HarmonyPatch(typeof(HeroController), "TakeDamage")]
        [HarmonyPostfix]
        public static void HeroController_TakeDamage_Postfix(HeroController __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;
            SpeedControlManager.ApplyPlayerSpeed();
        }

        /// <summary>
        /// After hero respawns, reapply all speeds.
        /// </summary>
        [HarmonyPatch(typeof(HeroController), "Respawn")]
        [HarmonyPostfix]
        public static void HeroController_Respawn_Postfix()
        {
            if (!SpeedControlConfig.IsEnabled) return;

            if (Plugin.Instance != null)
            {
                Plugin.Instance.StartCoroutine(ApplySpeedsNextFrame());
            }
        }

        #endregion

        #region NailSlash Patches (Player Attack Speed)

        /// <summary>
        /// After NailSlash plays, modify the clip FPS for attack speed.
        /// </summary>
        [HarmonyPatch(typeof(NailSlash), "PlaySlash")]
        [HarmonyPostfix]
        public static void NailSlash_PlaySlash_Postfix(NailSlash __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectivePlayerAttack;
            if (Mathf.Approximately(mult, 1f)) return;

            // Use reflection to access tk2dSpriteAnimator
            ApplyTk2dSpeedMultiplier(__instance.gameObject, mult);
        }

        #endregion

        #region Walker Patches (Enemy Movement Speed)

        /// <summary>
        /// When Walker starts, capture original speeds.
        /// </summary>
        [HarmonyPatch(typeof(Walker), "Start")]
        [HarmonyPostfix]
        public static void Walker_Start_Postfix(Walker __instance)
        {
            if (!_originalWalkerSpeeds.ContainsKey(__instance))
            {
                _originalWalkerSpeeds[__instance] = (__instance.walkSpeedL, __instance.walkSpeedR);
            }

            ApplyWalkerSpeed(__instance);
        }

        /// <summary>
        /// After Walker updates walking, apply speed multiplier.
        /// </summary>
        [HarmonyPatch(typeof(Walker), "UpdateWalking")]
        [HarmonyPostfix]
        public static void Walker_UpdateWalking_Postfix(Walker __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectiveEnemyMovement;
            if (Mathf.Approximately(mult, 1f)) return;

            // Get rigidbody and scale horizontal velocity
            var rb = __instance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                var vel = rb.linearVelocity;
                if (!_originalWalkerSpeeds.TryGetValue(__instance, out var original))
                {
                    original = (__instance.walkSpeedL, __instance.walkSpeedR);
                }

                // Apply multiplier to horizontal velocity
                if (vel.x > 0)
                    rb.linearVelocity = new Vector2(original.right * mult, vel.y);
                else if (vel.x < 0)
                    rb.linearVelocity = new Vector2(original.left * mult, vel.y);
            }
        }

        /// <summary>
        /// When Walker begins walking, apply speed to animator too.
        /// </summary>
        [HarmonyPatch(typeof(Walker), "BeginWalking")]
        [HarmonyPostfix]
        public static void Walker_BeginWalking_Postfix(Walker __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float animMult = SpeedControlConfig.EffectiveEnemyAttack;
            if (!Mathf.Approximately(animMult, 1f))
            {
                ApplyTk2dSpeedMultiplier(__instance.gameObject, animMult);
            }
        }

        private static void ApplyWalkerSpeed(Walker walker)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectiveEnemyMovement;
            if (_originalWalkerSpeeds.TryGetValue(walker, out var original))
            {
                walker.walkSpeedL = original.left * mult;
                walker.walkSpeedR = original.right * mult;
            }
        }

        #endregion

        #region Crawler Patches (Enemy Movement Speed)

        /// <summary>
        /// After Crawler starts crawling, apply speed multiplier to animator.
        /// </summary>
        [HarmonyPatch(typeof(Crawler), "StartCrawling", typeof(bool))]
        [HarmonyPostfix]
        public static void Crawler_StartCrawling_Postfix(Crawler __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float animMult = SpeedControlConfig.EffectiveEnemyAttack;
            if (!Mathf.Approximately(animMult, 1f))
            {
                ApplyTk2dSpeedMultiplier(__instance.gameObject, animMult);
            }
        }

        #endregion

        #region HealthManager Patches (Enemy Attack Speed)

        /// <summary>
        /// When any enemy is enabled, apply animation speed.
        /// </summary>
        [HarmonyPatch(typeof(HealthManager), "OnEnable")]
        [HarmonyPostfix]
        public static void HealthManager_OnEnable_Postfix(HealthManager __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float animMult = SpeedControlConfig.EffectiveEnemyAttack;
            if (!Mathf.Approximately(animMult, 1f))
            {
                ApplyTk2dSpeedMultiplier(__instance.gameObject, animMult);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Apply speed multiplier to tk2dSpriteAnimator via reflection.
        /// </summary>
        private static void ApplyTk2dSpeedMultiplier(GameObject go, float mult)
        {
            if (_tk2dAnimatorType == null || _clipFpsProperty == null) return;

            try
            {
                var animComponent = go.GetComponent(_tk2dAnimatorType);
                if (animComponent == null)
                {
                    animComponent = go.GetComponentInChildren(_tk2dAnimatorType);
                }

                if (animComponent != null && _currentClipProperty != null)
                {
                    var currentClip = _currentClipProperty.GetValue(animComponent);
                    if (currentClip != null)
                    {
                        float currentFps = (float)_clipFpsProperty.GetValue(animComponent);
                        _clipFpsProperty.SetValue(animComponent, currentFps * mult);
                    }
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"ApplyTk2dSpeedMultiplier failed: {e.Message}");
            }
        }

        private static bool IsHeroObject(GameObject go)
        {
            if (go == null) return false;

            var hero = HeroController.instance;
            if (hero == null) return false;

            return go == hero.gameObject || go.transform.IsChildOf(hero.transform);
        }

        private static System.Collections.IEnumerator ApplySpeedsNextFrame()
        {
            yield return null;
            SpeedControlManager.ApplyAllSpeeds();
        }

        /// <summary>
        /// Reset all Walker speeds to original.
        /// </summary>
        public static void ResetWalkerSpeeds()
        {
            foreach (var kvp in _originalWalkerSpeeds)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.walkSpeedL = kvp.Value.left;
                    kvp.Key.walkSpeedR = kvp.Value.right;
                }
            }
        }

        #endregion
    }
}
