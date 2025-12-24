using UnityEngine;
using System.Collections.Generic;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Efficient environment speed controller.
    /// Only applies changes when speed multiplier changes, not continuously.
    /// </summary>
    public class EnvironmentSpeedController : MonoBehaviour
    {
        private float _lastAppliedMult = 1f;

        // Cache reflection once
        private static System.Type _tk2dAnimatorType;
        private static System.Reflection.PropertyInfo _globalTimeScaleProp;
        private static bool _reflectionInitialized = false;

        void Start()
        {
            InitializeReflection();
        }

        void LateUpdate()
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EnvironmentSpeed;

            // Only apply when multiplier actually changed
            if (Mathf.Abs(mult - _lastAppliedMult) > 0.001f)
            {
                ApplyToAllEnvironment(mult);
                _lastAppliedMult = mult;
            }
        }

        private void ApplyToAllEnvironment(float mult)
        {
            ApplyToParticleSystems(mult);
            ApplyToAnimators(mult);
            ApplyToTk2dAnimators(mult);
        }

        private void ApplyToParticleSystems(float mult)
        {
            // Use cached array to reduce GC
            var particles = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            foreach (var ps in particles)
            {
                if (ps == null) continue;
                if (IsPlayerOrEnemy(ps.gameObject)) continue;

                var main = ps.main;
                main.simulationSpeed = mult;
            }
        }

        private void ApplyToAnimators(float mult)
        {
            var animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (var anim in animators)
            {
                if (anim == null) continue;
                if (IsPlayerOrEnemy(anim.gameObject)) continue;

                anim.speed = mult;
            }
        }

        private void ApplyToTk2dAnimators(float mult)
        {
            if (_tk2dAnimatorType == null) return;

            try
            {
                var allTk2d = FindObjectsByType(_tk2dAnimatorType, FindObjectsSortMode.None);
                foreach (var anim in allTk2d)
                {
                    if (anim == null) continue;

                    var component = anim as Component;
                    if (component == null) continue;
                    if (IsPlayerOrEnemy(component.gameObject)) continue;

                    try
                    {
                        if (_globalTimeScaleProp != null)
                        {
                            _globalTimeScaleProp.SetValue(anim, mult);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private bool IsPlayerOrEnemy(GameObject go)
        {
            if (go == null) return false;

            // Check for hero
            var hero = HeroController.instance;
            if (hero != null && (go == hero.gameObject || go.transform.IsChildOf(hero.transform)))
                return true;

            // Check for enemy (HealthManager)
            var hm = go.GetComponentInParent<HealthManager>();
            if (hm != null) return true;

            return false;
        }

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;

            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                _tk2dAnimatorType = asm.GetType("tk2dSpriteAnimator");
                if (_tk2dAnimatorType != null)
                {
                    _globalTimeScaleProp = _tk2dAnimatorType.GetProperty("globalTimeScale");
                    break;
                }
            }

            _reflectionInitialized = true;
        }

        /// <summary>
        /// Force reapply current speed.
        /// </summary>
        public void ForceReapply()
        {
            ApplyToAllEnvironment(SpeedControlConfig.EnvironmentSpeed);
            _lastAppliedMult = SpeedControlConfig.EnvironmentSpeed;
        }

        /// <summary>
        /// Reset all affected objects back to normal speed.
        /// </summary>
        public void ResetAll()
        {
            ApplyToAllEnvironment(1f);
            _lastAppliedMult = 1f;
        }
    }
}
