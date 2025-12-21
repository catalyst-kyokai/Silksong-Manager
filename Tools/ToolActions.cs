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
            var toolManager = ToolItemManager.UnsafeInstance;
            if (toolManager == null)
            {
                Plugin.Log.LogWarning("ToolItemManager not available.");
                return;
            }

            toolManager.UnlockAllTools();
            Plugin.Log.LogInfo("Unlocked all tools.");
        }

        /// <summary>
        /// Unlock all crests.
        /// </summary>
        public static void UnlockAllCrests()
        {
            var toolManager = ToolItemManager.UnsafeInstance;
            if (toolManager == null)
            {
                Plugin.Log.LogWarning("ToolItemManager not available.");
                return;
            }

            toolManager.UnlockAllCrests();
            Plugin.Log.LogInfo("Unlocked all crests.");
        }

        /// <summary>
        /// Get list of all tools.
        /// </summary>
        public static List<ToolInfo> GetAllTools()
        {
            var result = new List<ToolInfo>();
            var toolManager = ToolItemManager.UnsafeInstance;
            if (toolManager == null) return result;

            var tools = toolManager.GetAllTools();
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
            var toolManager = ToolItemManager.UnsafeInstance;
            if (toolManager == null) return result;

            var tools = toolManager.GetUnlockedTools();
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
            var toolManager = ToolItemManager.UnsafeInstance;
            if (toolManager == null) return result;

            var crests = toolManager.GetAllCrests();
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
        /// Replenish all tools.
        /// </summary>
        public static void ReplenishAllTools()
        {
            var toolManager = ToolItemManager.UnsafeInstance;
            if (toolManager == null) return;

            toolManager.TryReplenishTools(0, 9999);
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
