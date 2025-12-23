using HarmonyLib;
using SilksongManager.Damage;
using UnityEngine;

namespace SilksongManager.Patches
{
    /// <summary>
    /// Harmony patches for custom damage system.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class DamagePatches
    {
        #region Fields

        /// <summary>Harmony instance for patching.</summary>
        private static Harmony _harmony;

        #endregion

        #region Public Methods

        /// <summary>
        /// Apply all damage patches.
        /// </summary>
        public static void Apply()
        {
            try
            {
                _harmony = new Harmony("com.catalyst.silksongmanager.damage");
                _harmony.PatchAll(typeof(DamagePatches));
                Plugin.Log.LogInfo("DamagePatches applied successfully");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"Failed to apply DamagePatches: {e.Message}");
            }
        }

        /// <summary>
        /// Remove all patches.
        /// </summary>
        public static void Remove()
        {
            _harmony?.UnpatchSelf();
        }

        #endregion

        #region Harmony Patches

        /// <summary>
        /// Prefix patch for HealthManager.TakeDamage to modify damage values.
        /// Handles custom damage types, multipliers, and negative damage (healing).
        /// </summary>
        [HarmonyPatch(typeof(HealthManager), "TakeDamage")]
        [HarmonyPrefix]
        public static void TakeDamage_Prefix(ref HitInstance hitInstance, HealthManager __instance)
        {
            try
            {
                DamageType? damageType = GetDamageType(hitInstance.AttackType);

                if (damageType == null)
                    return;

                if (!DamageSystem.IsCustomEnabled(damageType.Value) &&
                    Mathf.Approximately(DamageSystem.GetMultiplier(damageType.Value), 1f) &&
                    Mathf.Approximately(DamageSystem.GlobalMultiplier, 1f))
                    return;

                float baseDamage = hitInstance.DamageDealt;
                float modifiedDamage = DamageSystem.CalculateFinalDamage(damageType.Value, baseDamage);

                if (modifiedDamage < 0)
                {
                    int healAmount = Mathf.RoundToInt(-modifiedDamage);
                    __instance.hp += healAmount;
                    hitInstance.DamageDealt = 0;
                }
                else
                {
                    hitInstance.DamageDealt = Mathf.RoundToInt(modifiedDamage);
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"DamagePatches error: {e.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Maps game AttackTypes to our DamageType enum.
        /// </summary>
        /// <param name="attackType">The game's attack type.</param>
        /// <returns>The corresponding DamageType, or null if not mapped.</returns>
        private static DamageType? GetDamageType(AttackTypes attackType)
        {
            switch (attackType)
            {
                case AttackTypes.Nail:
                case AttackTypes.NailBeam:
                case AttackTypes.SharpShadow:
                case AttackTypes.Generic:
                    return DamageType.Nail;

                case AttackTypes.Heavy:
                case AttackTypes.Explosion:
                case AttackTypes.Acid:
                case AttackTypes.Fire:
                case AttackTypes.Lightning:
                case AttackTypes.Hunter:
                    return DamageType.Tool;

                case AttackTypes.Spell:
                    return DamageType.Spell;

                case AttackTypes.Trap:
                case AttackTypes.Coal:
                    return DamageType.Summon;

                default:
                    return null;
            }
        }

        #endregion
    }
}
