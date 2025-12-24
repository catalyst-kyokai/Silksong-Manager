using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Harmony patches for speed control.
    /// 
    /// Enemy Movement Speed = Animation speed only (no velocity changes to avoid physics issues)
    /// Enemy Attack Speed = Attack animation speed + projectile velocity
    /// 
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class SpeedControlPatches
    {
        #region Fields

        private static Harmony _harmony;

        // Reflection for tk2d
        private static System.Type _tk2dAnimatorType;
        private static System.Type _tk2dClipType;
        private static PropertyInfo _clipFpsProperty;
        private static PropertyInfo _currentClipProperty;
        private static FieldInfo _clipNameField;
        private static bool _reflectionInitialized = false;

        // Track which enemies have the scaler component
        private static HashSet<int> _enemiesWithScaler = new();

        // Movement animation names (to distinguish from attack anims)
        private static readonly HashSet<string> _movementAnimNames = new()
        {
            "Walk", "Run", "Fly", "Idle", "Turn", "Move",
            "walk", "run", "fly", "idle", "turn", "move",
            "Walking", "Running", "Flying", "Turning", "Moving",
            "walking", "running", "flying", "turning", "moving",
            "Crawl", "crawl", "Crawling", "crawling"
        };

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

                // Universal spawned object velocity scaling - patches ObjectPool.Spawn
                PatchObjectPoolSpawn(ref patchCount);

                // Enemy velocity scaling via EnemySpeedScaler component
                TryPatch(typeof(HealthManager), "OnEnable", nameof(HealthManager_OnEnable_Postfix), ref patchCount);

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
                // Patch Play() - no args
                var playNoArgs = AccessTools.Method(_tk2dAnimatorType, "Play", System.Type.EmptyTypes);
                if (playNoArgs != null)
                {
                    var postfix = typeof(SpeedControlPatches).GetMethod(nameof(Tk2d_PlayNoArgs_Postfix), BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(playNoArgs, postfix: new HarmonyMethod(postfix));
                    count++;
                    Plugin.Log.LogInfo("SpeedControl: Patched tk2d.Play()");
                }

                // Patch Play(string)
                var playString = AccessTools.Method(_tk2dAnimatorType, "Play", new[] { typeof(string) });
                if (playString != null)
                {
                    var postfix = typeof(SpeedControlPatches).GetMethod(nameof(Tk2d_Play_Postfix), BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(playString, postfix: new HarmonyMethod(postfix));
                    count++;
                    Plugin.Log.LogInfo("SpeedControl: Patched tk2d.Play(string)");
                }

                // Patch Play(tk2dSpriteAnimationClip)
                if (_tk2dClipType != null)
                {
                    var playClip = AccessTools.Method(_tk2dAnimatorType, "Play", new[] { _tk2dClipType });
                    if (playClip != null)
                    {
                        var postfix = typeof(SpeedControlPatches).GetMethod(nameof(Tk2d_PlayClip_Postfix), BindingFlags.Public | BindingFlags.Static);
                        _harmony.Patch(playClip, postfix: new HarmonyMethod(postfix));
                        count++;
                        Plugin.Log.LogInfo("SpeedControl: Patched tk2d.Play(clip)");
                    }

                    // Patch Play(tk2dSpriteAnimationClip, float, float)
                    var playClipFps = AccessTools.Method(_tk2dAnimatorType, "Play", new[] { _tk2dClipType, typeof(float), typeof(float) });
                    if (playClipFps != null)
                    {
                        var postfix = typeof(SpeedControlPatches).GetMethod(nameof(Tk2d_PlayClipFps_Postfix), BindingFlags.Public | BindingFlags.Static);
                        _harmony.Patch(playClipFps, postfix: new HarmonyMethod(postfix));
                        count++;
                        Plugin.Log.LogInfo("SpeedControl: Patched tk2d.Play(clip,float,float)");
                    }
                }

                // Patch PlayFromFrame
                var playFromFrame = AccessTools.Method(_tk2dAnimatorType, "PlayFromFrame", new[] { typeof(string), typeof(int) });
                if (playFromFrame != null)
                {
                    var postfix = typeof(SpeedControlPatches).GetMethod(nameof(Tk2d_PlayFromFrame_Postfix), BindingFlags.Public | BindingFlags.Static);
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

        private static void PatchObjectPoolSpawn(ref int count)
        {
            try
            {
                // Patch ObjectPool.Spawn - the main method all spawned objects go through
                var spawnMethod = AccessTools.Method(
                    typeof(ObjectPool),
                    "Spawn",
                    new[] { typeof(GameObject), typeof(Transform), typeof(Vector3), typeof(Quaternion), typeof(bool) }
                );

                if (spawnMethod != null)
                {
                    var postfix = typeof(SpeedControlPatches).GetMethod(nameof(ObjectPool_Spawn_Postfix), BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(spawnMethod, postfix: new HarmonyMethod(postfix));
                    count++;
                    Plugin.Log.LogInfo("SpeedControl: Patched ObjectPool.Spawn");
                }
                else
                {
                    Plugin.Log.LogWarning("SpeedControl: ObjectPool.Spawn method not found");
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"SpeedControl: ObjectPool.Spawn patch failed: {e.Message}");
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
                    if (_tk2dAnimatorType != null)
                    {
                        _tk2dClipType = asm.GetType("tk2dSpriteAnimationClip");
                        Plugin.Log.LogInfo($"SpeedControl: Found tk2d type in {asm.GetName().Name}");

                        // Log all Play methods
                        var methods = _tk2dAnimatorType.GetMethods();
                        foreach (var m in methods)
                        {
                            if (m.Name == "Play")
                            {
                                var parms = m.GetParameters();
                                var parmStr = string.Join(", ", System.Array.ConvertAll(parms, p => p.ParameterType.Name));
                                Plugin.Log.LogInfo($"SpeedControl: Found Play({parmStr})");
                            }
                        }
                        break;
                    }
                }

                if (_tk2dAnimatorType != null)
                {
                    _clipFpsProperty = _tk2dAnimatorType.GetProperty("ClipFps");
                    _currentClipProperty = _tk2dAnimatorType.GetProperty("CurrentClip");
                }

                if (_tk2dClipType != null)
                {
                    _clipNameField = _tk2dClipType.GetField("name");
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

        #region tk2d Animation Patches

        private static int _tk2dCallCount = 0;

        /// <summary>
        /// Play() - no args, uses current clip
        /// </summary>
        public static void Tk2d_PlayNoArgs_Postfix(object __instance)
        {
            ApplyAnimationSpeedToCurrentClip(__instance);
        }

        /// <summary>
        /// Play(string) - by name
        /// </summary>
        public static void Tk2d_Play_Postfix(object __instance, string name)
        {
            _tk2dCallCount++;
            if (_tk2dCallCount <= 10)
            {
                var comp = __instance as Component;
                Plugin.Log.LogInfo($"SpeedControl: tk2d.Play called #{_tk2dCallCount}: '{name}' on {comp?.gameObject?.name ?? "null"}");
            }
            ApplyAnimationSpeed(__instance, name);
        }

        /// <summary>
        /// Play(tk2dSpriteAnimationClip) - by clip object
        /// </summary>
        public static void Tk2d_PlayClip_Postfix(object __instance, object clip)
        {
            string clipName = GetClipName(clip);
            _tk2dCallCount++;
            if (_tk2dCallCount <= 10)
            {
                var comp = __instance as Component;
                Plugin.Log.LogInfo($"SpeedControl: tk2d.Play(clip) called #{_tk2dCallCount}: '{clipName}' on {comp?.gameObject?.name ?? "null"}");
            }
            ApplyAnimationSpeed(__instance, clipName);
        }

        /// <summary>
        /// Play(tk2dSpriteAnimationClip, float, float) - with fps
        /// </summary>
        public static void Tk2d_PlayClipFps_Postfix(object __instance, object clip)
        {
            string clipName = GetClipName(clip);
            _tk2dCallCount++;
            if (_tk2dCallCount <= 10)
            {
                var comp = __instance as Component;
                Plugin.Log.LogInfo($"SpeedControl: tk2d.Play(clip,fps) called #{_tk2dCallCount}: '{clipName}' on {comp?.gameObject?.name ?? "null"}");
            }
            ApplyAnimationSpeed(__instance, clipName);
        }

        public static void Tk2d_PlayFromFrame_Postfix(object __instance, string name)
        {
            _tk2dCallCount++;
            if (_tk2dCallCount <= 10)
            {
                var comp = __instance as Component;
                Plugin.Log.LogInfo($"SpeedControl: tk2d.PlayFromFrame called #{_tk2dCallCount}: '{name}' on {comp?.gameObject?.name ?? "null"}");
            }
            ApplyAnimationSpeed(__instance, name);
        }

        private static void ApplyAnimationSpeed(object animator, string animName)
        {
            if (!SpeedControlConfig.IsEnabled) return;
            if (_clipFpsProperty == null) return;

            try
            {
                var component = animator as Component;
                if (component == null) return;

                var go = component.gameObject;

                // Skip hero (handled separately)
                if (IsHeroObject(go)) return;

                // Check if this is an enemy
                bool isEnemy = IsEnemyObject(go);
                if (!isEnemy)
                {
                    // Environment animation
                    float envMult = SpeedControlConfig.EnvironmentSpeed;
                    if (!Mathf.Approximately(envMult, 1f))
                    {
                        ApplyFpsMult(animator, envMult);
                    }
                    return;
                }

                // For enemies, determine if this is a movement or attack animation
                bool isMovementAnim = IsMovementAnimation(animName);

                float mult = 1f;
                if (isMovementAnim)
                {
                    // Movement animation - use movement speed
                    mult = SpeedControlConfig.EffectiveEnemyMovement;
                }
                else
                {
                    // Attack/other animation - use attack speed
                    mult = SpeedControlConfig.EffectiveEnemyAttack;
                    // Debug: log attack animations to verify they're being detected
                    if (!Mathf.Approximately(mult, 1f))
                    {
                        Plugin.Log.LogInfo($"SpeedControl: Attack anim '{animName}' on {go.name}, mult={mult}");
                    }
                }

                if (!Mathf.Approximately(mult, 1f))
                {
                    ApplyFpsMult(animator, mult);
                }
            }
            catch { }
        }

        private static void ApplyFpsMult(object animator, float mult)
        {
            try
            {
                float currentFps = (float)_clipFpsProperty.GetValue(animator);
                if (currentFps > 0)
                {
                    _clipFpsProperty.SetValue(animator, currentFps * mult);
                }
            }
            catch { }
        }

        private static bool IsMovementAnimation(string animName)
        {
            if (string.IsNullOrEmpty(animName)) return false;

            // Check if animation name contains any movement keywords
            foreach (var moveName in _movementAnimNames)
            {
                if (animName.Contains(moveName))
                    return true;
            }

            return false;
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

        #region Enemy Velocity Scaling

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
                }
                _enemiesWithScaler.Add(id);
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

        private static HashSet<int> _spawnedWithScaler = new();

        /// <summary>
        /// Universal postfix for ObjectPool.Spawn - adds ProjectileSpeedScaler to all spawned objects with Rigidbody2D
        /// </summary>
        public static void ObjectPool_Spawn_Postfix(GameObject __result)
        {
            if (__result == null) return;
            if (!SpeedControlConfig.IsEnabled) return;

            int id = __result.GetInstanceID();
            if (_spawnedWithScaler.Contains(id)) return;
            _spawnedWithScaler.Add(id);

            // Add ProjectileSpeedScaler if object has Rigidbody2D and is NOT a player object
            var rb = __result.GetComponent<Rigidbody2D>();
            if (rb != null && !IsHeroObject(__result))
            {
                var existing = __result.GetComponent<ProjectileSpeedScaler>();
                if (existing == null)
                {
                    __result.AddComponent<ProjectileSpeedScaler>();
                }
            }
        }

        #endregion

        #region Helper Methods

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

        private static string GetClipName(object clip)
        {
            if (clip == null || _clipNameField == null) return "";
            try
            {
                return _clipNameField.GetValue(clip) as string ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static void ApplyAnimationSpeedToCurrentClip(object animator)
        {
            if (_currentClipProperty == null) return;
            try
            {
                var currentClip = _currentClipProperty.GetValue(animator);
                string clipName = GetClipName(currentClip);
                ApplyAnimationSpeed(animator, clipName);
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
