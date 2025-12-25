using UnityEngine;

namespace SilksongManager.Currency
{
    /// <summary>
    /// Actions related to currency management (Geo, Shell Shards).
    /// Provides methods to add, set, and query player currency.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class CurrencyActions
    {
        #region Geo Management

        /// <summary>
        /// Adds the specified amount of geo to the player.
        /// </summary>
        /// <param name="amount">Amount of geo to add.</param>
        public static void AddGeo(int amount)
        {
            var pd = Plugin.PD;
            if (pd == null)
            {
                Plugin.Log.LogWarning("Cannot add geo: not in game.");
                return;
            }

            pd.geo += amount;
            CurrencyManager.AddGeoToCounter(amount);
            Plugin.Log.LogInfo($"Added {amount} geo. Current: {pd.geo}");
        }

        /// <summary>
        /// Sets the player's geo to a specific amount.
        /// </summary>
        /// <param name="amount">Target geo amount (minimum 0).</param>
        public static void SetGeo(int amount)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            pd.geo = Mathf.Max(0, amount);
            Plugin.Log.LogInfo($"Set geo to {pd.geo}");
        }

        /// <summary>
        /// Removes the specified amount of geo from the player.
        /// </summary>
        /// <param name="amount">Amount of geo to remove.</param>
        public static void TakeGeo(int amount)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            pd.geo = Mathf.Max(0, pd.geo - amount);
            Plugin.Log.LogInfo($"Removed {amount} geo. Current: {pd.geo}");
        }

        #endregion

        #region Shell Shards Management

        /// <summary>
        /// Adds shell shards to the player.
        /// </summary>
        /// <param name="amount">Number of shards to add.</param>
        public static void AddShards(int amount)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            pd.ShellShards += amount;
            Plugin.Log.LogInfo($"Added {amount} shards. Current: {pd.ShellShards}");
        }

        /// <summary>
        /// Sets shell shards to a specific amount.
        /// </summary>
        /// <param name="amount">Target shard count (minimum 0).</param>
        public static void SetShards(int amount)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            pd.ShellShards = Mathf.Max(0, amount);
            Plugin.Log.LogInfo($"Set shards to {pd.ShellShards}");
        }

        /// <summary>
        /// Removes shell shards from the player.
        /// </summary>
        /// <param name="amount">Number of shards to remove.</param>
        public static void TakeShards(int amount)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            pd.ShellShards = Mathf.Max(0, pd.ShellShards - amount);
            Plugin.Log.LogInfo($"Removed {amount} shards. Current: {pd.ShellShards}");
        }

        #endregion

        #region Currency Query

        /// <summary>
        /// Gets the current currency information.
        /// </summary>
        /// <returns>CurrencyInfo struct with current geo and shard counts.</returns>
        public static CurrencyInfo GetCurrencyInfo()
        {
            var pd = Plugin.PD;
            if (pd == null)
            {
                return new CurrencyInfo();
            }

            return new CurrencyInfo
            {
                Geo = pd.geo,
                ShellShards = pd.ShellShards
            };
        }

        #endregion
    }

    /// <summary>
    /// Currency information data structure.
    /// </summary>
    public struct CurrencyInfo
    {
        /// <summary>Current geo amount.</summary>
        public int Geo;
        /// <summary>Current shell shard count.</summary>
        public int ShellShards;
    }
}
