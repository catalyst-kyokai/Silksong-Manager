using System.Collections.Generic;
using System.Linq;

namespace SilksongManager.Inventory
{
    /// <summary>
    /// Actions related to crest management.
    /// Provides methods to unlock, lock, equip, and query crests.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class CrestActions
    {
        /// <summary>Hunter crest name - the starting crest that cannot be fully removed.</summary>
        public const string HunterCrestName = "Hunter";

        #region Unlock/Lock Methods

        /// <summary>
        /// Unlocks a specific crest by name.
        /// </summary>
        /// <param name="crestName">Name of the crest to unlock.</param>
        /// <returns>True if successful, false if crest not found.</returns>
        public static bool UnlockCrest(string crestName)
        {
            var crest = ToolItemManager.GetCrestByName(crestName);
            if (crest == null)
            {
                Plugin.Log.LogWarning($"Crest not found: {crestName}");
                return false;
            }

            crest.Unlock();
            Plugin.Log.LogInfo($"Unlocked crest: {crestName}");
            return true;
        }

        /// <summary>
        /// Locks/resets a specific crest by name.
        /// Hunter crest is reset to initial state, others are fully locked.
        /// </summary>
        /// <param name="crestName">Name of the crest to lock/reset.</param>
        /// <returns>True if successful, false if crest not found.</returns>
        public static bool LockCrest(string crestName)
        {
            var pd = Plugin.PD;
            if (pd == null) return false;

            // Get base crest (without upgrades)
            var crest = GetBaseCrest(crestName);
            if (crest == null)
            {
                Plugin.Log.LogWarning($"Crest not found: {crestName}");
                return false;
            }

            // Hunter crest cannot be fully removed, only reset
            if (crest.name.StartsWith(HunterCrestName))
            {
                ResetCrestToInitialState(crest.name);
                Plugin.Log.LogInfo($"Reset Hunter crest to initial state");
                return true;
            }

            // Lock all versions of this crest (base + upgrades)
            LockCrestChain(crest.name);

            // If this was the equipped crest, switch to Hunter
            if (pd.CurrentCrestID != null && pd.CurrentCrestID.StartsWith(crestName))
            {
                EquipCrest(HunterCrestName);
            }

            Plugin.Log.LogInfo($"Locked crest: {crestName}");
            return true;
        }

        /// <summary>
        /// Unlocks all crests.
        /// </summary>
        public static void UnlockAllCrests()
        {
            ToolItemManager.UnlockAllCrests();
            Plugin.Log.LogInfo("Unlocked all crests.");
        }

        /// <summary>
        /// Resets all crests (locks non-Hunter crests, resets Hunter to initial state).
        /// </summary>
        public static void ResetAllCrests()
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            var crests = ToolItemManager.GetAllCrests();
            foreach (var crest in crests)
            {
                if (crest == null) continue;

                var baseName = GetBaseCrestName(crest.name);
                if (baseName == HunterCrestName)
                {
                    ResetCrestToInitialState(crest.name);
                }
                else
                {
                    LockCrestChain(crest.name);
                }
            }

            // Equip Hunter crest
            EquipCrest(HunterCrestName);
            Plugin.Log.LogInfo("Reset all crests.");
        }

        #endregion

        #region Equip Methods

        /// <summary>
        /// Equips a specific crest.
        /// </summary>
        /// <param name="crestName">Name of the crest to equip.</param>
        /// <returns>True if successful.</returns>
        public static bool EquipCrest(string crestName)
        {
            var crest = ToolItemManager.GetCrestByName(crestName);
            if (crest == null)
            {
                // Try to find base version
                var allCrests = ToolItemManager.GetAllCrests();
                crest = allCrests.FirstOrDefault(c => c != null && c.name.StartsWith(crestName));
            }

            if (crest == null || !crest.IsUnlocked)
            {
                Plugin.Log.LogWarning($"Cannot equip crest: {crestName} (not found or not unlocked)");
                return false;
            }

            ToolItemManager.SetEquippedCrest(crest.name);
            Plugin.Log.LogInfo($"Equipped crest: {crest.name}");
            return true;
        }

        /// <summary>
        /// Gets the currently equipped crest name.
        /// </summary>
        public static string GetEquippedCrestName()
        {
            var pd = Plugin.PD;
            return pd?.CurrentCrestID ?? "";
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets information about all crests (base versions only).
        /// </summary>
        public static List<CrestInfo> GetAllCrests()
        {
            var result = new List<CrestInfo>();
            var crests = ToolItemManager.GetAllCrests();
            var addedBaseNames = new HashSet<string>();

            foreach (var crest in crests)
            {
                if (crest == null) continue;

                var baseName = GetBaseCrestName(crest.name);
                if (addedBaseNames.Contains(baseName)) continue;
                addedBaseNames.Add(baseName);

                // Find highest unlocked version
                var highestVersion = GetHighestUnlockedVersion(baseName);
                bool isUnlocked = highestVersion != null;

                result.Add(new CrestInfo
                {
                    Name = baseName,
                    DisplayName = baseName,
                    IsUnlocked = isUnlocked,
                    CurrentVersion = highestVersion?.name ?? baseName,
                    IsEquipped = Plugin.PD?.CurrentCrestID?.StartsWith(baseName) ?? false
                });
            }

            return result;
        }

        /// <summary>
        /// Gets the unlock status of a specific crest.
        /// </summary>
        public static bool IsCrestUnlocked(string crestName)
        {
            var crest = ToolItemManager.GetCrestByName(crestName);
            if (crest == null)
            {
                // Try to find any version
                var allCrests = ToolItemManager.GetAllCrests();
                crest = allCrests.FirstOrDefault(c => c != null && c.name.StartsWith(crestName) && c.IsUnlocked);
            }
            return crest?.IsUnlocked ?? false;
        }

        #endregion

        #region Helper Methods

        private static string GetBaseCrestName(string crestName)
        {
            // Remove version suffixes like "_V2", "_V3", etc.
            var underscoreIndex = crestName.LastIndexOf('_');
            if (underscoreIndex > 0 && crestName.Length > underscoreIndex + 1)
            {
                var suffix = crestName.Substring(underscoreIndex + 1);
                if (suffix.StartsWith("V") && suffix.Length > 1)
                {
                    return crestName.Substring(0, underscoreIndex);
                }
            }
            return crestName;
        }

        private static ToolCrest GetBaseCrest(string crestName)
        {
            var allCrests = ToolItemManager.GetAllCrests();
            var baseName = GetBaseCrestName(crestName);

            // Find base version (one that doesn't have previousVersion)
            foreach (var crest in allCrests)
            {
                if (crest != null && crest.name.StartsWith(baseName) && crest.IsBaseVersion)
                {
                    return crest;
                }
            }

            // Fallback: return any matching crest
            return allCrests.FirstOrDefault(c => c != null && c.name.StartsWith(baseName));
        }

        private static ToolCrest GetHighestUnlockedVersion(string baseName)
        {
            var allCrests = ToolItemManager.GetAllCrests();
            ToolCrest highest = null;

            foreach (var crest in allCrests)
            {
                if (crest == null) continue;
                if (!crest.name.StartsWith(baseName)) continue;
                if (!crest.IsUnlocked) continue;

                if (highest == null || crest.name.Length > highest.name.Length)
                {
                    highest = crest;
                }
            }

            return highest;
        }

        private static void ResetCrestToInitialState(string crestName)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            var baseName = GetBaseCrestName(crestName);
            var allCrests = ToolItemManager.GetAllCrests();

            // Lock all upgraded versions, keep only base
            foreach (var crest in allCrests)
            {
                if (crest == null) continue;
                if (!crest.name.StartsWith(baseName)) continue;

                if (!crest.IsBaseVersion)
                {
                    // Lock upgrade version
                    var data = pd.ToolEquips.GetData(crest.name);
                    data.IsUnlocked = false;
                    pd.ToolEquips.SetData(crest.name, data);
                }
            }
        }

        private static void LockCrestChain(string crestName)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            var baseName = GetBaseCrestName(crestName);
            var allCrests = ToolItemManager.GetAllCrests();

            foreach (var crest in allCrests)
            {
                if (crest == null) continue;
                if (!crest.name.StartsWith(baseName)) continue;

                var data = pd.ToolEquips.GetData(crest.name);
                data.IsUnlocked = false;
                pd.ToolEquips.SetData(crest.name, data);
            }
        }

        #endregion
    }

    /// <summary>
    /// Crest information structure.
    /// </summary>
    public struct CrestInfo
    {
        /// <summary>Base name of the crest.</summary>
        public string Name;
        /// <summary>Display name.</summary>
        public string DisplayName;
        /// <summary>Whether any version is unlocked.</summary>
        public bool IsUnlocked;
        /// <summary>Current version name (highest unlocked).</summary>
        public string CurrentVersion;
        /// <summary>Whether this crest is currently equipped.</summary>
        public bool IsEquipped;
    }
}
