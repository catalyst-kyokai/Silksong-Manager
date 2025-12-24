using UnityEngine;
using System.Collections.Generic;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Scales enemy velocities every frame to simulate time scale effect.
    /// Attached to enemies at runtime.
    /// </summary>
    public class EnemySpeedScaler : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private HealthManager _hm;
        private Vector2 _lastVelocity;
        private bool _isInitialized;

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
            if (Mathf.Approximately(moveMult, 1f)) return;

            Vector2 currentVel = _rb.linearVelocity;

            // Only scale if there's actual movement
            if (currentVel.sqrMagnitude < 0.01f) return;

            // Scale the velocity
            // We need to be careful not to compound - only scale if velocity changed from last frame
            if (!_isInitialized || currentVel != _lastVelocity)
            {
                _rb.linearVelocity = currentVel * moveMult;
                _lastVelocity = _rb.linearVelocity;
                _isInitialized = true;
            }
        }
    }
}
