namespace LGSTrayHID.Protocol;

internal static class AppConstants
{
    [Obsolete("Use GlobalSettings.InitBackoff.GetTimeout() instead. This constant will be removed in a future version.")]
    public const int INIT_PING_TIMEOUT_MS = 5000;

    [Obsolete("Use GlobalSettings.FeatureEnumBackoff.GetTimeout() instead. This constant will be removed in a future version.")]
    public const int WRITE_READ_TIMEOUT_MS = 5000;

    // Battery query timeouts
    [Obsolete("Use GlobalSettings.BatteryBackoff.GetTimeout() instead. This constant will be removed in a future version.")]
    public const int UNIFIED_QueryTimeout = 5000;

    [Obsolete("Use GlobalSettings.BatteryBackoff.GetTimeout() instead. This constant will be removed in a future version.")]
    public const int UNIFIED_LEVEL_QueryTimeout = 5000;

    [Obsolete("Use GlobalSettings.BatteryBackoff.GetTimeout() instead. This constant will be removed in a future version.")]
    public const int BATT_VOLTAGE_QueryTimeout = 5000;

}
