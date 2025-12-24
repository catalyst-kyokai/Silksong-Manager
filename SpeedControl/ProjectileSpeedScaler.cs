using UnityEngine;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Scales projectile velocities based on enemy attack speed.
    /// Attached to projectiles at spawn time.
    /// </summary>
    public class ProjectileSpeedScaler : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private bool _hasScaled = false;
        private float _appliedMult = 1f;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        void Start()
        {
            // Try to scale on start (for projectiles that set velocity in Awake/OnEnable)
            TryScale();
        }

        void FixedUpdate()
        {
            // Keep trying to scale until we successfully do it
            if (!_hasScaled)
            {
                TryScale();
            }
            else
            {
                // If multiplier changed, reapply
                float currentMult = SpeedControlConfig.EffectiveEnemyAttack;
                if (Mathf.Abs(currentMult - _appliedMult) > 0.01f)
                {
                    // Undo old scaling and apply new
                    if (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.1f)
                    {
                        _rb.linearVelocity = (_rb.linearVelocity / _appliedMult) * currentMult;
                        _appliedMult = currentMult;
                    }
                }
            }
        }

        private void TryScale()
        {
            if (_rb == null) return;
            if (!SpeedControlConfig.IsEnabled) return;

            float mult = SpeedControlConfig.EffectiveEnemyAttack;
            if (Mathf.Approximately(mult, 1f))
            {
                _hasScaled = true;
                _appliedMult = 1f;
                return;
            }

            Vector2 vel = _rb.linearVelocity;
            if (vel.sqrMagnitude < 0.1f) return; // Wait for velocity to be set

            _rb.linearVelocity = vel * mult;
            _hasScaled = true;
            _appliedMult = mult;
        }
    }
}
