using System.Collections.Generic;

namespace SilksongManager.Inventory
{
    /// <summary>
    /// Silk ability types available in the game.
    /// </summary>
    public enum SilkAbility
    {
        Dash,
        Walljump,
        DoubleJump,
        SuperJump,
        SilkSpecial,
        NeedleThrow,
        ThreadSphere,
        Parry,
        HarpoonDash,
        SilkCharge,
        SilkBomb,
        Needolin
    }

    /// <summary>
    /// Actions related to silk abilities management.
    /// Provides methods to grant, revoke, and query silk abilities.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class AbilityActions
    {
        /// <summary>
        /// Display names for abilities.
        /// </summary>
        private static readonly Dictionary<SilkAbility, string> AbilityNames = new Dictionary<SilkAbility, string>
        {
            { SilkAbility.Dash, "Dash" },
            { SilkAbility.Walljump, "Wall Jump" },
            { SilkAbility.DoubleJump, "Double Jump" },
            { SilkAbility.SuperJump, "Super Jump" },
            { SilkAbility.SilkSpecial, "Silk Special" },
            { SilkAbility.NeedleThrow, "Needle Throw" },
            { SilkAbility.ThreadSphere, "Thread Sphere" },
            { SilkAbility.Parry, "Parry" },
            { SilkAbility.HarpoonDash, "Harpoon Dash" },
            { SilkAbility.SilkCharge, "Silk Charge" },
            { SilkAbility.SilkBomb, "Silk Bomb" },
            { SilkAbility.Needolin, "Needolin" }
        };

        #region Grant/Revoke Methods

        /// <summary>
        /// Grants a silk ability to the player.
        /// </summary>
        /// <param name="ability">The ability to grant.</param>
        /// <returns>True if successful.</returns>
        public static bool GrantAbility(SilkAbility ability)
        {
            var pd = Plugin.PD;
            if (pd == null) return false;

            SetAbilityState(pd, ability, true);
            Plugin.Log.LogInfo($"Granted ability: {GetAbilityName(ability)}");
            return true;
        }

        /// <summary>
        /// Revokes a silk ability from the player.
        /// </summary>
        /// <param name="ability">The ability to revoke.</param>
        /// <returns>True if successful.</returns>
        public static bool RevokeAbility(SilkAbility ability)
        {
            var pd = Plugin.PD;
            if (pd == null) return false;

            SetAbilityState(pd, ability, false);
            Plugin.Log.LogInfo($"Revoked ability: {GetAbilityName(ability)}");
            return true;
        }

        /// <summary>
        /// Grants all silk abilities to the player.
        /// </summary>
        public static void GrantAllAbilities()
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            foreach (SilkAbility ability in System.Enum.GetValues(typeof(SilkAbility)))
            {
                SetAbilityState(pd, ability, true);
            }
            Plugin.Log.LogInfo("Granted all abilities.");
        }

        /// <summary>
        /// Revokes all silk abilities from the player.
        /// </summary>
        public static void RevokeAllAbilities()
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            foreach (SilkAbility ability in System.Enum.GetValues(typeof(SilkAbility)))
            {
                SetAbilityState(pd, ability, false);
            }
            Plugin.Log.LogInfo("Revoked all abilities.");
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets the status of all silk abilities.
        /// </summary>
        public static List<AbilityInfo> GetAllAbilities()
        {
            var result = new List<AbilityInfo>();
            var pd = Plugin.PD;

            foreach (SilkAbility ability in System.Enum.GetValues(typeof(SilkAbility)))
            {
                result.Add(new AbilityInfo
                {
                    Ability = ability,
                    Name = GetAbilityName(ability),
                    IsUnlocked = pd != null && GetAbilityState(pd, ability)
                });
            }

            return result;
        }

        /// <summary>
        /// Checks if a specific ability is unlocked.
        /// </summary>
        public static bool HasAbility(SilkAbility ability)
        {
            var pd = Plugin.PD;
            return pd != null && GetAbilityState(pd, ability);
        }

        /// <summary>
        /// Gets the display name for an ability.
        /// </summary>
        public static string GetAbilityName(SilkAbility ability)
        {
            return AbilityNames.TryGetValue(ability, out var name) ? name : ability.ToString();
        }

        #endregion

        #region Helper Methods

        private static bool GetAbilityState(PlayerData pd, SilkAbility ability)
        {
            return ability switch
            {
                SilkAbility.Dash => pd.hasDash,
                SilkAbility.Walljump => pd.hasWalljump,
                SilkAbility.DoubleJump => pd.hasDoubleJump,
                SilkAbility.SuperJump => pd.hasSuperJump,
                SilkAbility.SilkSpecial => pd.hasSilkSpecial,
                SilkAbility.NeedleThrow => pd.hasNeedleThrow,
                SilkAbility.ThreadSphere => pd.hasThreadSphere,
                SilkAbility.Parry => pd.hasParry,
                SilkAbility.HarpoonDash => pd.hasHarpoonDash,
                SilkAbility.SilkCharge => pd.hasSilkCharge,
                SilkAbility.SilkBomb => pd.hasSilkBomb,
                SilkAbility.Needolin => pd.hasNeedolin,
                _ => false
            };
        }

        private static void SetAbilityState(PlayerData pd, SilkAbility ability, bool state)
        {
            switch (ability)
            {
                case SilkAbility.Dash:
                    pd.hasDash = state;
                    break;
                case SilkAbility.Walljump:
                    pd.hasWalljump = state;
                    break;
                case SilkAbility.DoubleJump:
                    pd.hasDoubleJump = state;
                    break;
                case SilkAbility.SuperJump:
                    pd.hasSuperJump = state;
                    break;
                case SilkAbility.SilkSpecial:
                    pd.hasSilkSpecial = state;
                    break;
                case SilkAbility.NeedleThrow:
                    pd.hasNeedleThrow = state;
                    break;
                case SilkAbility.ThreadSphere:
                    pd.hasThreadSphere = state;
                    break;
                case SilkAbility.Parry:
                    pd.hasParry = state;
                    break;
                case SilkAbility.HarpoonDash:
                    pd.hasHarpoonDash = state;
                    break;
                case SilkAbility.SilkCharge:
                    pd.hasSilkCharge = state;
                    break;
                case SilkAbility.SilkBomb:
                    pd.hasSilkBomb = state;
                    break;
                case SilkAbility.Needolin:
                    pd.hasNeedolin = state;
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// Ability information structure.
    /// </summary>
    public struct AbilityInfo
    {
        /// <summary>The ability enum value.</summary>
        public SilkAbility Ability;
        /// <summary>Display name.</summary>
        public string Name;
        /// <summary>Whether the ability is unlocked.</summary>
        public bool IsUnlocked;
    }
}
