namespace SilksongManager.SpeedControl
{
    /// <summary>
    /// Configuration for speed control system.
    /// Stores all speed multipliers and provides persistence.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class SpeedControlConfig
    {
        #region Global Speed

        /// <summary>Target global game speed (Time.timeScale).</summary>
        public static float GlobalSpeed { get; set; } = 1f;

        #endregion

        #region Player Speed

        /// <summary>Player movement speed multiplier.</summary>
        public static float PlayerMovementSpeed { get; set; } = 1f;

        /// <summary>Player attack speed multiplier.</summary>
        public static float PlayerAttackSpeed { get; set; } = 1f;

        /// <summary>Combined player speed multiplier (affects both movement and attacks).</summary>
        public static float PlayerAllSpeed { get; set; } = 1f;

        /// <summary>Gets effective player movement multiplier.</summary>
        public static float EffectivePlayerMovement => PlayerMovementSpeed * PlayerAllSpeed;

        /// <summary>Gets effective player attack multiplier.</summary>
        public static float EffectivePlayerAttack => PlayerAttackSpeed * PlayerAllSpeed;

        #endregion

        #region Enemy Speed

        /// <summary>Enemy movement speed multiplier.</summary>
        public static float EnemyMovementSpeed { get; set; } = 1f;

        /// <summary>Enemy attack speed multiplier.</summary>
        public static float EnemyAttackSpeed { get; set; } = 1f;

        /// <summary>Combined enemy speed multiplier.</summary>
        public static float EnemyAllSpeed { get; set; } = 1f;

        /// <summary>Gets effective enemy movement multiplier.</summary>
        public static float EffectiveEnemyMovement => EnemyMovementSpeed * EnemyAllSpeed;

        /// <summary>Gets effective enemy attack multiplier.</summary>
        public static float EffectiveEnemyAttack => EnemyAttackSpeed * EnemyAllSpeed;

        #endregion

        #region State

        /// <summary>Whether speed control system is active.</summary>
        public static bool IsEnabled { get; set; } = true;

        /// <summary>Original RUN_SPEED value from HeroController.</summary>
        internal static float OriginalRunSpeed { get; set; } = 0f;

        /// <summary>Original WALK_SPEED value from HeroController.</summary>
        internal static float OriginalWalkSpeed { get; set; } = 0f;

        /// <summary>Whether original values have been captured.</summary>
        internal static bool OriginalsCaptured { get; set; } = false;

        #endregion

        #region Methods

        /// <summary>
        /// Reset all speed multipliers to default (1.0).
        /// </summary>
        public static void ResetAll()
        {
            GlobalSpeed = 1f;
            PlayerMovementSpeed = 1f;
            PlayerAttackSpeed = 1f;
            PlayerAllSpeed = 1f;
            EnemyMovementSpeed = 1f;
            EnemyAttackSpeed = 1f;
            EnemyAllSpeed = 1f;
        }

        /// <summary>
        /// Check if any speed is modified from default.
        /// </summary>
        public static bool IsAnyModified()
        {
            return GlobalSpeed != 1f ||
                   PlayerMovementSpeed != 1f ||
                   PlayerAttackSpeed != 1f ||
                   PlayerAllSpeed != 1f ||
                   EnemyMovementSpeed != 1f ||
                   EnemyAttackSpeed != 1f ||
                   EnemyAllSpeed != 1f;
        }

        #endregion
    }
}
