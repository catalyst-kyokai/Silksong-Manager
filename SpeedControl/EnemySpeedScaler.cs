using UnityEngine;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Scales enemy velocities to simulate time scale effect.
    /// Only affects Rigidbody2D based movement within walking speed range.
    /// </summary>
    public class EnemySpeedScaler : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private HealthManager _hm;

        // Walking speed threshold - above this is likely a dash/attack
        // Increased to accommodate flying enemies which often move slower
        private const float WALK_SPEED_MAX = 25f;

        // Minimum speed to consider for scaling
        private const float MIN_SPEED = 0.5f;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _hm = GetComponent<HealthManager>();
            if (_hm == null)
                _hm = GetComponentInParent<HealthManager>();
        }

        void FixedUpdate()
        {
            if (_rb == null || _hm == null) return;
            if (!SpeedControlConfig.IsEnabled) return;

            float moveMult = SpeedControlConfig.EffectiveEnemyMovement;

            // Skip if multiplier is 1
            if (Mathf.Approximately(moveMult, 1f)) return;

            Vector2 currentVel = _rb.linearVelocity;
            float speed = currentVel.magnitude;

            // Ignore very small movements
            if (speed < MIN_SPEED) return;

            // Only scale walking-speed movements, not dashes/attacks
            // Dashes typically have much higher velocities
            if (speed > WALK_SPEED_MAX) return;

            // Scale velocity - since game continuously sets velocity each frame,
            // we just need to multiply the current value
            // The game will reset it next frame, and we'll scale again
            Vector2 direction = currentVel.normalized;
            float scaledSpeed = speed * moveMult;

            // Clamp to prevent exceeding dash thresholds
            scaledSpeed = Mathf.Min(scaledSpeed, WALK_SPEED_MAX);

            _rb.linearVelocity = direction * scaledSpeed;
        }
    }
}
