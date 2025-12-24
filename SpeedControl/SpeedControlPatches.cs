using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Harmony patches for speed control.
    /// Enemy speed works like Time.timeScale: scales both velocity and animations.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class SpeedControlPatches
    {
        #region Fields

        private static Harmony _harmony;

        // Reflection for tk2d
        private static System.Type _tk2dAnimatorType;
        private static PropertyInfo _clipFpsProperty;
        private static bool _reflectionInitialized = false;

        // Track which enemies have the scaler component
        private static HashSet<int> _enemiesWithScaler = new();

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

                // Enemy initialization - add speed scaler component
                TryPatch(typeof(HealthManager), "OnEnable", nameof(HealthManager_OnEnable_Postfix), ref patchCount);

                // Enemy projectile velocity
                TryPatch(typeof(EnemyBullet), "OnEnable", nameof(EnemyBullet_OnEnable_Postfix), ref patchCount);

                // Patch tk2d for animation speed
                PatchTk2dAnimator(ref patchCount);

                Plugin.Log.LogInfo($"SpeedControlPatches: {patchCount} patches applied");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"SpeedControlPatches failed: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void PatchTk2dAnimator(ref int count)
        {
            if (_tk2dAnimatorType == null) return;

            try
            {
                // Patch Play(string)
                var playString = AccessTools.Method(_tk2dAnimatorType, "Play", new[] { typeof(string) });
                if (playString != null)
                {
                    var postfix = typeof(SpeedControlPatches).GetMethod(nameof(Tk2d_Play_Postfix), BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(playString, postfix: new HarmonyMethod(postfix));
                    count++;
                    Plugin.Log.LogInfo("SpeedControl: Patched tk2d.Play(string)");
                }

                // Patch PlayFromFrame
                var playFromFrame = AccessTools.Method(_tk2dAnimatorType, "PlayFromFrame", new[] { typeof(string), typeof(int) });
                if (playFromFrame != null)
                {
                    var postfix = typeof(SpeedControlPatches).GetMethod(nameof(Tk2d_Play_Postfix), BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(playFromFrame, postfix: new HarmonyMethod(postfix));
                    count++;
                    Plugin.Log.LogInfo("SpeedControl: Patched tk2d.PlayFromFrame");
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"SpeedControl: tk2d patch failed: {e.Message}");
            }
        }

        private static void TryPatch(System.Type targetType, string methodName, string patchName, ref int count)
        {
            try
            {
                var original = AccessTools.Method(targetType, methodName);
                if (original == null)
                {
                    Plugin.Log.LogWarning($"SpeedControl: {targetType.Name}.{methodName} not found");
                    return;
                }

                var patch = typeof(SpeedControlPatches).GetMethod(patchName, BindingFlags.Public | BindingFlags.Static);
                if (patch == null) return;

                _harmony.Patch(original, postfix: new HarmonyMethod(patch));
                count++;
                Plugin.Log.LogInfo($"SpeedControl: Patched {targetType.Name}.{methodName}");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"SpeedControl: {targetType.Name}.{methodName} failed: {e.Message}");
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
                }
            }
            catch { }

            _reflectionInitialized = true;
        }

        public static void Remove()
        {
            _harmony?.UnpatchSelf();
            _enemiesWithScaler.Clear();
        }

        #endregion

        #region tk2d Animation Patch

        /// <summary>
        /// Apply animation speed on every Play call.
        /// </summary>
        public static void Tk2d_Play_Postfix(object __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;
            if (_clipFpsProperty == null) return;

            try
            {
                var component = __instance as Component;
                if (component == null) return;

                var go = component.gameObject;
                float mult = GetSpeedMultiplierForObject(go);

                if (Mathf.Approximately(mult, 1f)) return;

                float currentFps = (float)_clipFpsProperty.GetValue(__instance);
                if (currentFps > 0)
                {
                    _clipFpsProperty.SetValue(__instance, currentFps * mult);
                }
            }
            catch { }
        }

        #endregion

        #region Enemy Patches

        /// <summary>
        /// Add EnemySpeedScaler component to enemies for velocity scaling.
        /// </summary>
        public static void HealthManager_OnEnable_Postfix(HealthManager __instance)
        {
            if (__instance == null) return;

            int id = __instance.gameObject.GetInstanceID();

            // Add EnemySpeedScaler component if not already present
            if (!_enemiesWithScaler.Contains(id))
            {
                var existing = __instance.GetComponent<EnemySpeedScaler>();
                if (existing == null)
                {
                    __instance.gameObject.AddComponent<EnemySpeedScaler>();
                    Plugin.Log.LogInfo($"SpeedControl: Added scaler to {__instance.gameObject.name}");
                }
                _enemiesWithScaler.Add(id);
            }
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

        #region Enemy Projectile Patches

        public static void EnemyBullet_OnEnable_Postfix(EnemyBullet __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectiveEnemyAttack;
            if (Mathf.Approximately(mult, 1f)) return;

            if (Plugin.Instance != null)
            {
                Plugin.Instance.StartCoroutine(ScaleBulletVelocity(__instance, mult));
            }
        }

        private static System.Collections.IEnumerator ScaleBulletVelocity(EnemyBullet bullet, float mult)
        {
            yield return null;

            if (bullet == null) yield break;

            var rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                rb.linearVelocity *= mult;
                Plugin.Log.LogInfo($"SpeedControl: Scaled bullet velocity by {mult}");
            }
        }

        #endregion

        #region Helper Methods

        private static float GetSpeedMultiplierForObject(GameObject go)
        {
            if (IsHeroObject(go))
            {
                return 1f; // Hero handled separately
            }
            else if (IsEnemyObject(go))
            {
                // For enemies, use the combined effective speed
                float move = SpeedControlConfig.EffectiveEnemyMovement;
                float attack = SpeedControlConfig.EffectiveEnemyAttack;
                return Mathf.Max(move, attack);
            }
            else
            {
                return SpeedControlConfig.EnvironmentSpeed;
            }
        }

        private static bool IsHeroObject(GameObject go)
        {
            if (go == null) return false;
            var hero = HeroController.instance;
            if (hero == null) return false;
            return go == hero.gameObject || go.transform.IsChildOf(hero.transform);
        }

        private static bool IsEnemyObject(GameObject go)
        {
            if (go == null) return false;
            var hm = go.GetComponentInParent<HealthManager>();
            return hm != null;
        }

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

        private static System.Collections.IEnumerator ApplySpeedsNextFrame()
        {
            yield return null;
            SpeedControlManager.ApplyAllSpeeds();
        }

        public static void ResetWalkerSpeeds()
        {
            _enemiesWithScaler.Clear();
        }

        #endregion
    }
}
