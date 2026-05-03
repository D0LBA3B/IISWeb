using System.Text.RegularExpressions;
using IISWeb.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Web.Administration;

namespace IISWeb.Services;

public class IisPoolService : IIisPoolService
{
    private static readonly Regex PoolNameRegex =
        new(@"^[A-Za-z0-9 _\.\-]{1,64}$", RegexOptions.Compiled);

    private readonly IOptionsMonitor<AppOptions> _opt;
    private readonly ILogger<IisPoolService> _logger;

    public IisPoolService(IOptionsMonitor<AppOptions> opt, ILogger<IisPoolService> logger)
    {
        _opt = opt;
        _logger = logger;
    }

    private static bool IsValidName(string? name) =>
        !string.IsNullOrEmpty(name) && PoolNameRegex.IsMatch(name);

    public bool IsAllowed(string poolName)
    {
        if (!IsValidName(poolName)) return false;
        var allowed = _opt.CurrentValue.AllowedAppPools;
        if (allowed is null || allowed.Length == 0)
            return true; // No whitelist configured -> all pools visible/actionable
        return allowed.Any(a => string.Equals(a, poolName, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<AppPoolInfo> List()
    {
        using var sm = new ServerManager();
        var allowed = _opt.CurrentValue.AllowedAppPools;
        IEnumerable<ApplicationPool> pools = sm.ApplicationPools;
        if (allowed is { Length: > 0 })
        {
            var set = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);
            pools = pools.Where(p => set.Contains(p.Name));
        }
        return pools
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(MapInfo)
            .ToList();
    }

    public AppPoolInfo? Get(string poolName)
    {
        if (!IsAllowed(poolName)) return null;
        using var sm = new ServerManager();
        var p = sm.ApplicationPools[poolName];
        return p is null ? null : MapInfo(p);
    }

    public PoolActionResult Start(string poolName) => Run(poolName, AuditActionStart, p =>
    {
        if (p.State == ObjectState.Started || p.State == ObjectState.Starting)
            return ("Pool is already running.", p.State.ToString());
        var newState = p.Start();
        return ($"Pool started (state: {newState}).", newState.ToString());
    });

    public PoolActionResult Stop(string poolName) => Run(poolName, AuditActionStop, p =>
    {
        if (p.State == ObjectState.Stopped || p.State == ObjectState.Stopping)
            return ("Pool is already stopped.", p.State.ToString());
        var newState = p.Stop();
        return ($"Pool stopped (state: {newState}).", newState.ToString());
    });

    public PoolActionResult Recycle(string poolName) => Run(poolName, AuditActionRecycle, p =>
    {
        if (p.State != ObjectState.Started)
            throw new InvalidOperationException($"Cannot recycle a pool in state '{p.State}'. Pool must be started.");
        p.Recycle();
        return ("Recycle requested.", p.State.ToString());
    });

    private const string AuditActionStart = "Start";
    private const string AuditActionStop = "Stop";
    private const string AuditActionRecycle = "Recycle";

    private PoolActionResult Run(string poolName, string action, Func<ApplicationPool, (string Message, string NewState)> op)
    {
        if (!IsValidName(poolName))
            return new PoolActionResult(false, "Invalid pool name.");

        if (!IsAllowed(poolName))
            return new PoolActionResult(false, "Pool not allowed by configuration.");

        try
        {
            using var sm = new ServerManager();
            var pool = sm.ApplicationPools[poolName];
            if (pool is null)
                return new PoolActionResult(false, "Pool not found.");

            var (message, newState) = op(pool);
            return new PoolActionResult(true, message, newState);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized to {Action} pool {Pool}", action, poolName);
            return new PoolActionResult(false,
                "Access denied. The Windows account running the IISWeb App Pool lacks IIS administration rights.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation: {Action} on pool {Pool}", action, poolName);
            return new PoolActionResult(false, ex.Message);
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            _logger.LogError(ex, "IIS COM error on {Action} of pool {Pool}", action, poolName);
            return new PoolActionResult(false, $"IIS error: 0x{ex.HResult:X8}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to {Action} pool {Pool}", action, poolName);
            return new PoolActionResult(false, "An unexpected error occurred. Check server logs.");
        }
    }

    private static AppPoolInfo MapInfo(ApplicationPool p) => new(
        p.Name,
        p.State.ToString(),
        p.ManagedRuntimeVersion ?? string.Empty,
        p.ProcessModel.IdentityType.ToString(),
        p.AutoStart);
}
