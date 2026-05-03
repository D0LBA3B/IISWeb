namespace IISWeb.Configuration;

public class AppOptions
{
    public const string SectionName = "App";

    public string[] AllowedAppPools { get; set; } = Array.Empty<string>();
    public int LoginMaxAttempts { get; set; } = 5;
    public int LoginLockoutMinutes { get; set; } = 15;
    public bool RequireHttps { get; set; } = true;
    public string SqlitePath { get; set; } = "App_Data/iisweb.db";
    public int SessionTimeoutMinutes { get; set; } = 60;

    /// <summary>
    /// Optional IP allow-list. Each entry can be a single IPv4/IPv6 address
    /// ("10.0.0.5", "::1") or a CIDR range ("10.0.0.0/8", "192.168.1.0/24",
    /// "fd00::/8"). An empty array disables IP filtering and lets every IP in.
    /// </summary>
    public string[] AllowedIpRanges { get; set; } = Array.Empty<string>();
}
