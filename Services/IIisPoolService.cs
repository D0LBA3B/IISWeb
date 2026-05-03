namespace IISWeb.Services;

public record AppPoolInfo(
    string Name,
    string State,
    string ManagedRuntimeVersion,
    string IdentityType,
    bool AutoStart);

public record PoolActionResult(bool Success, string Message, string? NewState = null);

public interface IIisPoolService
{
    bool IsAllowed(string poolName);
    IReadOnlyList<AppPoolInfo> List();
    AppPoolInfo? Get(string poolName);
    PoolActionResult Start(string poolName);
    PoolActionResult Stop(string poolName);
    PoolActionResult Recycle(string poolName);
}
