using UnityEngine;

namespace SilksongManager.Currency
{
    /// <summary>
    /// Actions related to currency management (Geo, Shards).
    /// </summary>
    public static class CurrencyActions
    {
        /// <summary>
        /// Add geo to the player.
        /// </summary>
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
        /// Set geo to specific amount.
        /// </summary>
        public static void SetGeo(int amount)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            pd.geo = Mathf.Max(0, amount);
            Plugin.Log.LogInfo($"Set geo to {pd.geo}");
        }

        /// <summary>
        /// Take geo from the player.
        /// </summary>
        public static void TakeGeo(int amount)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            pd.geo = Mathf.Max(0, pd.geo - amount);
            Plugin.Log.LogInfo($"Removed {amount} geo. Current: {pd.geo}");
        }

        /// <summary>
        /// Add shell shards to the player.
        /// </summary>
        public static void AddShards(int amount)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            pd.ShellShards += amount;
            Plugin.Log.LogInfo($"Added {amount} shards. Current: {pd.ShellShards}");
        }

        /// <summary>
        /// Set shell shards to specific amount.
        /// </summary>
        public static void SetShards(int amount)
        {
            var pd = Plugin.PD;
            if (pd == null) return;

            pd.ShellShards = Mathf.Max(0, amount);
            Plugin.Log.LogInfo($"Set shards to {pd.ShellShards}");
        }

        /// <summary>
        /// Get current currency info.
        /// </summary>
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
    }

    /// <summary>
    /// Currency information.
    /// </summary>
    public struct CurrencyInfo
    {
        public int Geo;
        public int ShellShards;
    }
}
