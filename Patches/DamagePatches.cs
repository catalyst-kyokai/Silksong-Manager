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
        private static Harmony _harmony;

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

        /// <summary>
        /// Prefix patch for HealthManager.TakeDamage to modify damage values.
        /// </summary>
        [HarmonyPatch(typeof(HealthManager), "TakeDamage")]
        [HarmonyPrefix]
        public static void TakeDamage_Prefix(ref HitInstance hitInstance, HealthManager __instance)
        {
            try
            {
                // Determine damage type from AttackType
                DamageType? damageType = GetDamageType(hitInstance.AttackType);

                if (damageType == null)
                    return;

                // Check if any custom damage is enabled for this type
                if (!DamageSystem.IsCustomEnabled(damageType.Value) &&
                    Mathf.Approximately(DamageSystem.GetMultiplier(damageType.Value), 1f) &&
                    Mathf.Approximately(DamageSystem.GlobalMultiplier, 1f))
                    return;

                // Calculate modified damage
                float baseDamage = hitInstance.DamageDealt;
                float modifiedDamage = DamageSystem.CalculateFinalDamage(damageType.Value, baseDamage);

                // Handle negative damage (healing)
                if (modifiedDamage < 0)
                {
                    // Heal the enemy instead of damaging
                    int healAmount = Mathf.RoundToInt(-modifiedDamage);
                    __instance.hp += healAmount;
                    hitInstance.DamageDealt = 0; // No actual damage

                    // Optional: log healing
                    // Plugin.Log.LogInfo($"Healed enemy for {healAmount} HP");
                }
                else
                {
                    // Apply modified damage
                    hitInstance.DamageDealt = Mathf.RoundToInt(modifiedDamage);
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"DamagePatches error: {e.Message}");
            }
        }

        /// <summary>
        /// Map AttackTypes to our DamageType enum.
        /// </summary>
        private static DamageType? GetDamageType(AttackTypes attackType)
        {
            switch (attackType)
            {
                // Nail attacks
                case AttackTypes.Nail:
                case AttackTypes.NailBeam:
                case AttackTypes.SharpShadow:
                case AttackTypes.Generic:
                    return DamageType.Nail;

                // Tool attacks (weapons equipped as tools)
                case AttackTypes.Heavy:
                case AttackTypes.Explosion:
                case AttackTypes.Acid:
                case AttackTypes.Fire:
                case AttackTypes.Lightning:
                case AttackTypes.Hunter:
                    return DamageType.Tool;

                // Spell attacks
                case AttackTypes.Spell:
                    return DamageType.Spell;

                // Summon/trap attacks
                case AttackTypes.Trap:
                case AttackTypes.Coal:
                    return DamageType.Summon;

                default:
                    return null;
            }
        }
    }
}
