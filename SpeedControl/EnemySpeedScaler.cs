using UnityEngine;
using System.Collections.Generic;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Scales enemy velocities intelligently.
    /// Only scales when velocity is in "walking" range (not dashes/attacks).
    /// </summary>
    public class EnemySpeedScaler : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private HealthManager _hm;

        // Base walking speed threshold - velocities above this are likely dashes/attacks
        private const float WALK_SPEED_THRESHOLD = 12f;

        // Track if we're currently in scaled movement
        private bool _wasMoving;
        private Vector2 _baseVelocity;

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

            // Only scale if multiplier is not 1
            if (Mathf.Approximately(moveMult, 1f))
            {
                _wasMoving = false;
                return;
            }

            Vector2 currentVel = _rb.linearVelocity;
            float speed = currentVel.magnitude;

            // Only scale if:
            // 1. There's actual movement
            // 2. Speed is below threshold (walking, not dashing)
            if (speed < 0.1f)
            {
                _wasMoving = false;
                return;
            }

            // Check if this is walking speed (not a dash)
            if (speed <= WALK_SPEED_THRESHOLD)
            {
                // This is walking - scale it
                Vector2 direction = currentVel.normalized;
                float scaledSpeed = speed * moveMult;

                // Clamp to prevent going faster than dash speed
                scaledSpeed = Mathf.Min(scaledSpeed, WALK_SPEED_THRESHOLD * moveMult);

                _rb.linearVelocity = direction * scaledSpeed;
                _wasMoving = true;
            }
            else
            {
                // This is a dash/attack - don't scale
                _wasMoving = false;
            }
        }
    }
}
