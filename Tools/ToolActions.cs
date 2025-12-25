using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilksongManager.Tools
{
    /// <summary>
    /// Actions related to tools and crests management.
    /// Provides methods to unlock, list, and replenish tools and crests.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class ToolActions
    {
        #region Unlock Methods
        /// <summary>
        /// Unlock all tools.
        /// </summary>
        public static void UnlockAllTools()
        {
            ToolItemManager.UnlockAllTools();
            Plugin.Log.LogInfo("Unlocked all tools.");
        }

        /// <summary>
        /// Unlock all crests.
        /// </summary>
        public static void UnlockAllCrests()
        {
            ToolItemManager.UnlockAllCrests();
            Plugin.Log.LogInfo("Unlocked all crests.");
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Get list of all tools.
        /// </summary>
        public static List<ToolInfo> GetAllTools()
        {
            var result = new List<ToolInfo>();
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

        #endregion

        #region Replenish Methods

        /// <summary>
        /// Replenishes all tools (at bench).
        /// </summary>
        public static void ReplenishAllTools()
        {
            ToolItemManager.TryReplenishTools(true, ToolItemManager.ReplenishMethod.Bench);
            Plugin.Log.LogInfo("Replenished all tools.");
        }

        #endregion

        #region Individual Tool Methods

        /// <summary>
        /// Unlocks a specific tool by name.
        /// </summary>
        /// <param name="toolName">Name of the tool to unlock.</param>
        /// <returns>True if successful.</returns>
        public static bool UnlockTool(string toolName)
        {
            var tool = ToolItemManager.GetToolByName(toolName);
            if (tool == null)
            {
                Plugin.Log.LogWarning($"Tool not found: {toolName}");
                return false;
            }

            tool.Get(true);
            Plugin.Log.LogInfo($"Unlocked tool: {toolName}");
            return true;
        }

        /// <summary>
        /// Locks a specific tool by name.
        /// </summary>
        /// <param name="toolName">Name of the tool to lock.</param>
        /// <returns>True if successful.</returns>
        public static bool LockTool(string toolName)
        {
            var tool = ToolItemManager.GetToolByName(toolName);
            if (tool == null)
            {
                Plugin.Log.LogWarning($"Tool not found: {toolName}");
                return false;
            }

            tool.Lock();
            Plugin.Log.LogInfo($"Locked tool: {toolName}");
            return true;
        }

        /// <summary>
        /// Locks all tools.
        /// </summary>
        public static void LockAllTools()
        {
            var tools = ToolItemManager.GetAllTools();
            foreach (var tool in tools)
            {
                if (tool != null && tool.IsUnlocked)
                {
                    tool.Lock();
                }
            }
            Plugin.Log.LogInfo("Locked all tools.");
        }

        /// <summary>
        /// Gets all tools that are not crests (regular tools only).
        /// </summary>
        public static List<ToolInfo> GetNonCrestTools()
        {
            var result = new List<ToolInfo>();
            var tools = ToolItemManager.GetAllTools();

            foreach (var tool in tools)
            {
                if (tool == null) continue;

                // Skip if this is a crest (ToolCrest inherits from ToolBase, not ToolItem directly)
                // ToolItem is what we want
                result.Add(new ToolInfo
                {
                    Name = tool.name,
                    IsUnlocked = tool.IsUnlocked
                });
            }

            return result;
        }

        #endregion
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
