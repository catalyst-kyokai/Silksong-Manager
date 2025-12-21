using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilksongManager.Tools
{
    /// <summary>
    /// Actions related to tools and crests management.
    /// </summary>
    public static class ToolActions
    {
        /// <summary>
        /// Unlock all tools.
        /// </summary>
        public static void UnlockAllTools()
        {
            // UnlockAllTools is a static method on ToolItemManager
            ToolItemManager.UnlockAllTools();
            Plugin.Log.LogInfo("Unlocked all tools.");
        }

        /// <summary>
        /// Unlock all crests.
        /// </summary>
        public static void UnlockAllCrests()
        {
            // UnlockAllCrests is a static method on ToolItemManager
            ToolItemManager.UnlockAllCrests();
            Plugin.Log.LogInfo("Unlocked all crests.");
        }

        /// <summary>
        /// Get list of all tools.
        /// </summary>
        public static List<ToolInfo> GetAllTools()
        {
            var result = new List<ToolInfo>();

            // GetAllTools is a static method
            var tools = ToolItemManager.GetAllTools();
            foreach (var tool in tools)
            {
                if (tool != null)
                {
                    result.Add(new ToolInfo
                    {
                        Name = tool.name,
                        IsUnlocked = tool.IsUnlocked
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Get list of unlocked tools.
        /// </summary>
        public static List<ToolInfo> GetUnlockedTools()
        {
            var result = new List<ToolInfo>();

            // GetUnlockedTools is a static method
            var tools = ToolItemManager.GetUnlockedTools();
            foreach (var tool in tools)
            {
                if (tool != null)
                {
                    result.Add(new ToolInfo
                    {
                        Name = tool.name,
                        IsUnlocked = true
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Get list of all crests.
        /// </summary>
        public static List<CrestInfo> GetAllCrests()
        {
            var result = new List<CrestInfo>();

            // GetAllCrests is a static method
            var crests = ToolItemManager.GetAllCrests();
            foreach (var crest in crests)
            {
                if (crest != null)
                {
                    result.Add(new CrestInfo
                    {
                        Name = crest.name,
                        IsUnlocked = crest.IsUnlocked
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Replenish all tools (at bench).
        /// </summary>
        public static void ReplenishAllTools()
        {
            // TryReplenishTools is a static method with (bool doReplenish, ReplenishMethod method)
            ToolItemManager.TryReplenishTools(true, ToolItemManager.ReplenishMethod.Bench);
            Plugin.Log.LogInfo("Replenished all tools.");
        }
    }

    /// <summary>
    /// Tool information.
    /// </summary>
    public struct ToolInfo
    {
        public string Name;
        public bool IsUnlocked;
    }

    /// <summary>
    /// Crest information.
    /// </summary>
    public struct CrestInfo
    {
        public string Name;
        public bool IsUnlocked;
    }
}
