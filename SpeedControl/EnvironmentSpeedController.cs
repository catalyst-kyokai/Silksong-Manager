using UnityEngine;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Controller that continuously applies environment speed to all appropriate objects.
    /// Handles Animators, tk2d Animators, Particle Systems, and physics.
    /// </summary>
    public class EnvironmentSpeedController : MonoBehaviour
    {
        private float _lastAppliedMult = 1f;
        private float _applyTimer = 0f;
        private const float APPLY_INTERVAL = 0.5f; // Apply every 0.5 seconds

        // Cache types for reflection
        private static System.Type _tk2dAnimatorType;
        private static System.Reflection.PropertyInfo _clipFpsProp;
        private static bool _reflectionInitialized = false;

        void Start()
        {
            InitializeReflection();
        }

        void Update()
        {
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EnvironmentSpeed;

            // Only apply when multiplier changed or at intervals
            _applyTimer -= Time.unscaledDeltaTime;
            if (_applyTimer <= 0f || Mathf.Abs(mult - _lastAppliedMult) > 0.01f)
            {
                ApplyToAllEnvironment(mult);
                _lastAppliedMult = mult;
                _applyTimer = APPLY_INTERVAL;
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
            if (_tk2dAnimatorType == null || _clipFpsProp == null) return;

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
                        // We need to track original FPS to properly scale
                        // For now, just set the speed property if available
                        var speedProp = _tk2dAnimatorType.GetProperty("globalTimeScale");
                        if (speedProp != null)
                        {
                            speedProp.SetValue(anim, mult);
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
                    _clipFpsProp = _tk2dAnimatorType.GetProperty("ClipFps");
                    break;
                }
            }

            _reflectionInitialized = true;
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
