using HarmonyLib;
using UnityEngine;
using System.Reflection;
using HutongGames.PlayMaker.Actions;

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

                // Enemy projectile velocity
                TryPatch(typeof(EnemyBullet), "OnEnable", nameof(EnemyBullet_OnEnable_Postfix), ref patchCount);

                // Enemy VELOCITY scaling via PlayMaker FSM actions
                TryPatch(typeof(SetVelocity2d), "DoSetVelocity", nameof(SetVelocity2d_DoSetVelocity_Postfix), ref patchCount);

                // Walker velocity scaling
                TryPatch(typeof(Walker), "BeginWalking", nameof(Walker_BeginWalking_Postfix), ref patchCount);
                TryPatch(typeof(Walker), "UpdateWalking", nameof(Walker_UpdateWalking_Postfix), ref patchCount);

                // Crawler velocity scaling
                TryPatchOverload(typeof(Crawler), "StartCrawling", new[] { typeof(bool) }, nameof(Crawler_StartCrawling_Postfix), ref patchCount);

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
                }

                // Patch PlayFromFrame
                var playFromFrame = AccessTools.Method(_tk2dAnimatorType, "PlayFromFrame", new[] { typeof(string), typeof(int) });
                if (playFromFrame != null)
                {
                    var postfix = typeof(SpeedControlPatches).GetMethod(nameof(Tk2d_Play_Postfix), BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(playFromFrame, postfix: new HarmonyMethod(postfix));
                    count++;
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

        private static void TryPatchOverload(System.Type targetType, string methodName, System.Type[] args, string patchName, ref int count)
        {
            try
            {
                var original = AccessTools.Method(targetType, methodName, args);
                if (original == null) return;

                var patch = typeof(SpeedControlPatches).GetMethod(patchName, BindingFlags.Public | BindingFlags.Static);
                if (patch == null) return;

                _harmony.Patch(original, postfix: new HarmonyMethod(patch));
                count++;
                Plugin.Log.LogInfo($"SpeedControl: Patched {targetType.Name}.{methodName}");
            }
            catch { }
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

        #region Velocity Scaling Patches

        /// <summary>
        /// Scale velocity set by PlayMaker SetVelocity2d action.
        /// </summary>
        public static void SetVelocity2d_DoSetVelocity_Postfix(SetVelocity2d __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectiveEnemyMovement;
            if (Mathf.Approximately(mult, 1f)) return;

            try
            {
                var go = __instance.Fsm.GetOwnerDefaultTarget(__instance.gameObject);
                if (go == null) return;

                // Only scale for enemies (objects with HealthManager)
                var hm = go.GetComponentInParent<HealthManager>();
                if (hm == null) return;

                // Don't scale hero
                if (IsHeroObject(go)) return;

                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity *= mult;
                }
            }
            catch { }
        }

        /// <summary>
        /// Scale Walker velocity after BeginWalking sets it.
        /// </summary>
        public static void Walker_BeginWalking_Postfix(Walker __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectiveEnemyMovement;
            if (Mathf.Approximately(mult, 1f)) return;

            var rb = __instance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                var vel = rb.linearVelocity;
                rb.linearVelocity = new Vector2(vel.x * mult, vel.y);
            }
        }

        /// <summary>
        /// Keep Walker velocity scaled during UpdateWalking.
        /// </summary>
        public static void Walker_UpdateWalking_Postfix(Walker __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectiveEnemyMovement;
            if (Mathf.Approximately(mult, 1f)) return;

            var rb = __instance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Walker continuously sets velocity, we need to scale it
                float targetSpeed = (__instance.walkSpeedR + Mathf.Abs(__instance.walkSpeedL)) / 2f;
                var vel = rb.linearVelocity;

                // Only scale if not already scaled
                if (Mathf.Abs(vel.x) > 0.1f && Mathf.Abs(vel.x) < targetSpeed * mult * 1.1f)
                {
                    rb.linearVelocity = new Vector2(vel.x * mult, vel.y);
                }
            }
        }

        /// <summary>
        /// Scale Crawler velocity.
        /// </summary>
        public static void Crawler_StartCrawling_Postfix(Crawler __instance)
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectiveEnemyMovement;
            if (Mathf.Approximately(mult, 1f)) return;

            var rb = __instance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity *= mult;
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

        public static void ResetWalkerSpeeds() { }

        #endregion
    }
}
