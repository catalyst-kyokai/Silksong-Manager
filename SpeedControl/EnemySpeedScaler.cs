using UnityEngine;

namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Scales enemy velocities to simulate time scale effect.
    /// Tracks original velocity and scales relative to it.
    /// </summary>
    public class EnemySpeedScaler : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private HealthManager _hm;

        // Walking speed threshold - above this is likely a dash/attack
        private const float WALK_SPEED_MAX = 15f;

        // Track the last velocity we set (to detect when game changes it)
        private Vector2 _ourLastVelocity;
        private Vector2 _originalVelocity;
        private bool _hasOriginal;

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

            Vector2 currentVel = _rb.linearVelocity;
            float speed = currentVel.magnitude;

            // Ignore very small movements
            if (speed < 0.1f)
            {
                _hasOriginal = false;
                return;
            }

            // Check if this is a new velocity (game set it) or our scaled velocity
            bool velocityChangedByGame = !_hasOriginal ||
                Vector2.Distance(currentVel, _ourLastVelocity) > 0.5f;

            if (velocityChangedByGame)
            {
                // Game set a new velocity - this is the original
                _originalVelocity = currentVel;
                _hasOriginal = true;
            }

            float originalSpeed = _originalVelocity.magnitude;

            // Only scale walking-speed movements, not dashes
            if (originalSpeed > WALK_SPEED_MAX)
            {
                // This is a dash/attack - don't scale
                _ourLastVelocity = currentVel;
                return;
            }

            // Skip if multiplier is 1
            if (Mathf.Approximately(moveMult, 1f))
            {
                _ourLastVelocity = currentVel;
                return;
            }

            // Calculate scaled velocity based on ORIGINAL (unscaled) velocity
            Vector2 scaledVel = _originalVelocity * moveMult;

            // Apply scaled velocity
            _rb.linearVelocity = scaledVel;
            _ourLastVelocity = scaledVel;
        }
    }
}
