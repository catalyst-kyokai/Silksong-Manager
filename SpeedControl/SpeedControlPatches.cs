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

        /// <summary>Debug logging enabled.</summary>
        private static bool _debugLogging = true;

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

                // Manually patch each method to provide better error diagnostics
                var patchCount = 0;

                // GameManager patches
                TryPatch(typeof(GameManager), "UnpauseGame", nameof(GameManager_UnpauseGame_Postfix), ref patchCount);
                TryPatch(typeof(GameManager), "SetTimeScale", nameof(GameManager_SetTimeScale_Postfix), ref patchCount);

                // HeroController patches
                TryPatch(typeof(HeroController), "Start", nameof(HeroController_Start_Postfix), ref patchCount);
                TryPatch(typeof(HeroController), "TakeDamage", nameof(HeroController_TakeDamage_Postfix), ref patchCount);
                TryPatch(typeof(HeroController), "Respawn", nameof(HeroController_Respawn_Postfix), ref patchCount);

                // NailSlash patches
                TryPatch(typeof(NailSlash), "PlaySlash", nameof(NailSlash_PlaySlash_Postfix), ref patchCount);

                // Walker patches
                TryPatch(typeof(Walker), "Start", nameof(Walker_Start_Postfix), ref patchCount);
                TryPatch(typeof(Walker), "UpdateWalking", nameof(Walker_UpdateWalking_Postfix), ref patchCount);
                TryPatch(typeof(Walker), "BeginWalking", nameof(Walker_BeginWalking_Postfix), ref patchCount);

                // Crawler patches
                TryPatchOverload(typeof(Crawler), "StartCrawling", new[] { typeof(bool) }, nameof(Crawler_StartCrawling_Postfix), ref patchCount);

                // HealthManager patches
                TryPatch(typeof(HealthManager), "OnEnable", nameof(HealthManager_OnEnable_Postfix), ref patchCount);

                Plugin.Log.LogInfo($"SpeedControlPatches: {patchCount} patches applied successfully");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Failed to apply SpeedControlPatches: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void TryPatch(System.Type targetType, string methodName, string postfixName, ref int count)
        {
            try
            {
                var original = AccessTools.Method(targetType, methodName);
                if (original == null)
                {
                    Plugin.Log.LogWarning($"SpeedControl: Method {targetType.Name}.{methodName} not found");
                    return;
                }

                var postfix = typeof(SpeedControlPatches).GetMethod(postfixName, BindingFlags.Public | BindingFlags.Static);
                if (postfix == null)
                {
                    Plugin.Log.LogWarning($"SpeedControl: Postfix {postfixName} not found");
                    return;
                }

                _harmony.Patch(original, postfix: new HarmonyMethod(postfix));
                count++;
                if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: Patched {targetType.Name}.{methodName}");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"SpeedControl: Failed to patch {targetType.Name}.{methodName}: {e.Message}");
            }
        }

        private static void TryPatchOverload(System.Type targetType, string methodName, System.Type[] argTypes, string postfixName, ref int count)
        {
            try
            {
                var original = AccessTools.Method(targetType, methodName, argTypes);
                if (original == null)
                {
                    string argStr = argTypes != null ? string.Join(", ", System.Array.ConvertAll(argTypes, t => t.Name)) : "";
                    Plugin.Log.LogWarning($"SpeedControl: Method {targetType.Name}.{methodName}({argStr}) not found");
                    return;
                }

                var postfix = typeof(SpeedControlPatches).GetMethod(postfixName, BindingFlags.Public | BindingFlags.Static);
                if (postfix == null)
                {
                    Plugin.Log.LogWarning($"SpeedControl: Postfix {postfixName} not found");
                    return;
                }

                _harmony.Patch(original, postfix: new HarmonyMethod(postfix));
                count++;
                if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: Patched {targetType.Name}.{methodName}");
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
                Plugin.Log.LogInfo("SpeedControl: Initializing tk2d reflection...");

                // Find tk2dSpriteAnimator type
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    _tk2dAnimatorType = asm.GetType("tk2dSpriteAnimator");
                    if (_tk2dAnimatorType != null)
                    {
                        Plugin.Log.LogInfo($"SpeedControl: Found tk2dSpriteAnimator in assembly {asm.GetName().Name}");
                        break;
                    }
                }

                if (_tk2dAnimatorType != null)
                {
                    _clipFpsProperty = _tk2dAnimatorType.GetProperty("ClipFps");
                    _currentClipProperty = _tk2dAnimatorType.GetProperty("CurrentClip");

                    Plugin.Log.LogInfo($"SpeedControl: tk2d reflection - ClipFps: {_clipFpsProperty != null}, CurrentClip: {_currentClipProperty != null}");

                    // Log all properties for debugging
                    if (_debugLogging)
                    {
                        var props = _tk2dAnimatorType.GetProperties();
                        Plugin.Log.LogInfo($"SpeedControl: tk2dSpriteAnimator has {props.Length} properties:");
                        foreach (var prop in props)
                        {
                            Plugin.Log.LogInfo($"  - {prop.Name} ({prop.PropertyType.Name})");
                        }
                    }
                }
                else
                {
                    Plugin.Log.LogWarning("SpeedControl: tk2dSpriteAnimator type not found in any assembly");
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"SpeedControl: Reflection init failed: {e.Message}");
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

        public static void GameManager_UnpauseGame_Postfix()
        {
            if (!SpeedControlConfig.IsEnabled) return;

            if (SpeedControlConfig.GlobalSpeed != 1f)
            {
                Time.timeScale = SpeedControlConfig.GlobalSpeed;
                if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: Restored timeScale to {SpeedControlConfig.GlobalSpeed}");
            }
        }

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

        public static void HeroController_Start_Postfix(HeroController __instance)
        {
            if (_debugLogging) Plugin.Log.LogInfo("SpeedControl: HeroController.Start called");

            if (!SpeedControlConfig.OriginalsCaptured)
            {
                SpeedControlConfig.OriginalRunSpeed = __instance.RUN_SPEED;
                SpeedControlConfig.OriginalWalkSpeed = __instance.WALK_SPEED;
                SpeedControlConfig.OriginalsCaptured = true;
                Plugin.Log.LogInfo($"SpeedControl: Captured original speeds - Run: {SpeedControlConfig.OriginalRunSpeed}, Walk: {SpeedControlConfig.OriginalWalkSpeed}");
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

        #region NailSlash Patches (Player Attack Speed)

        public static void NailSlash_PlaySlash_Postfix(NailSlash __instance)
        {
            if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: NailSlash.PlaySlash called on {__instance.gameObject.name}");

            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectivePlayerAttack;
            if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: Player attack mult = {mult}");

            if (Mathf.Approximately(mult, 1f)) return;

            // Try via reflection
            bool applied = ApplyTk2dSpeedMultiplier(__instance.gameObject, mult);
            if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: NailSlash tk2d speed applied: {applied}");
        }

        #endregion

        #region Walker Patches (Enemy Movement Speed)

        public static void Walker_Start_Postfix(Walker __instance)
        {
            if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: Walker.Start called on {__instance.gameObject.name}");

            if (!_originalWalkerSpeeds.ContainsKey(__instance))
            {
                _originalWalkerSpeeds[__instance] = (__instance.walkSpeedL, __instance.walkSpeedR);
                if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: Captured Walker speeds - L: {__instance.walkSpeedL}, R: {__instance.walkSpeedR}");
            }

            ApplyWalkerSpeed(__instance);
        }

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

            // Also apply to animator
            ApplyTk2dSpeedMultiplier(__instance.gameObject, mult);
        }

        public static void Walker_BeginWalking_Postfix(Walker __instance)
        {
            if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: Walker.BeginWalking called on {__instance.gameObject.name}");

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
                if (_debugLogging && !Mathf.Approximately(mult, 1f))
                {
                    Plugin.Log.LogInfo($"SpeedControl: Applied Walker speed mult {mult} - L: {walker.walkSpeedL}, R: {walker.walkSpeedR}");
                }
            }
        }

        #endregion

        #region Crawler Patches (Enemy Movement Speed)

        public static void Crawler_StartCrawling_Postfix(Crawler __instance)
        {
            if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: Crawler.StartCrawling called on {__instance.gameObject.name}");

            if (!SpeedControlConfig.IsEnabled) return;

            float animMult = SpeedControlConfig.EffectiveEnemyAttack;
            if (!Mathf.Approximately(animMult, 1f))
            {
                ApplyTk2dSpeedMultiplier(__instance.gameObject, animMult);
            }
        }

        #endregion

        #region HealthManager Patches (Enemy Attack Speed)

        public static void HealthManager_OnEnable_Postfix(HealthManager __instance)
        {
            if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: HealthManager.OnEnable called on {__instance.gameObject.name}");

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
        /// Returns true if successful.
        /// </summary>
        private static bool ApplyTk2dSpeedMultiplier(GameObject go, float mult)
        {
            if (_tk2dAnimatorType == null)
            {
                if (_debugLogging) Plugin.Log.LogWarning("SpeedControl: tk2dAnimatorType is null");
                return false;
            }

            try
            {
                var animComponent = go.GetComponent(_tk2dAnimatorType);
                if (animComponent == null)
                {
                    animComponent = go.GetComponentInChildren(_tk2dAnimatorType);
                }

                if (animComponent == null)
                {
                    if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: No tk2dSpriteAnimator found on {go.name}");
                    return false;
                }

                if (_clipFpsProperty == null)
                {
                    if (_debugLogging) Plugin.Log.LogWarning("SpeedControl: ClipFps property is null");
                    return false;
                }

                // Check if we can get/set ClipFps
                float currentFps = (float)_clipFpsProperty.GetValue(animComponent);
                float newFps = currentFps * mult;
                _clipFpsProperty.SetValue(animComponent, newFps);

                if (_debugLogging) Plugin.Log.LogInfo($"SpeedControl: Applied tk2d speed on {go.name}: {currentFps} -> {newFps}");
                return true;
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"SpeedControl: ApplyTk2dSpeedMultiplier failed on {go.name}: {e.Message}");
                return false;
            }
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
            Plugin.Log.LogInfo($"SpeedControl: Reset {_originalWalkerSpeeds.Count} Walker speeds");
        }

        #endregion
    }
}
