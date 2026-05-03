using System.Net;
using System.Net.Sockets;
using IISWeb.Configuration;
using Microsoft.Extensions.Options;

namespace IISWeb.Middleware;

/// <summary>
/// Refuses any request whose remote IP is not in <see cref="AppOptions.AllowedIpRanges"/>.
/// Empty list = no filtering. CIDR and bare IPs are both accepted.
///
/// Runs after <c>UseForwardedHeaders</c> so that <see cref="HttpContext.Connection"/>
/// reports the original client IP when the app is behind a trusted proxy / IIS.
/// </summary>
public class IpAllowListMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<AppOptions> _opt;
    private readonly ILogger<IpAllowListMiddleware> _logger;

    private readonly object _gate = new();
    private string[]? _cachedSource;
    private List<IPNetwork> _cachedNetworks = new();

    public IpAllowListMiddleware(
        RequestDelegate next,
        IOptionsMonitor<AppOptions> opt,
        ILogger<IpAllowListMiddleware> logger)
    {
        _next = next;
        _opt = opt;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var configured = _opt.CurrentValue.AllowedIpRanges ?? Array.Empty<string>();

        if (configured.Length == 0)
        {
            await _next(ctx);
            return;
        }

        var networks = GetNetworks(configured);
        if (networks.Count == 0)
        {
            // All entries failed to parse: fail closed and shout in the log.
            _logger.LogError(
                "AllowedIpRanges is configured but no entry could be parsed. Denying all requests.");
            await DenyAsync(ctx);
            return;
        }

        var ip = ctx.Connection.RemoteIpAddress;
        if (ip is null)
        {
            _logger.LogWarning("Denying request with unknown remote IP. Path={Path}", ctx.Request.Path);
            await DenyAsync(ctx);
            return;
        }

        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        foreach (var net in networks)
        {
            if (net.Contains(ip))
            {
                await _next(ctx);
                return;
            }
        }

        _logger.LogWarning(
            "Denied request from {Ip} (not in AllowedIpRanges). Path={Path} UA={Ua}",
            ip, ctx.Request.Path, ctx.Request.Headers.UserAgent.ToString());
        await DenyAsync(ctx);
    }

    private static async Task DenyAsync(HttpContext ctx)
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        ctx.Response.ContentType = "text/plain; charset=utf-8";
        await ctx.Response.WriteAsync("Forbidden");
    }

    private List<IPNetwork> GetNetworks(string[] source)
    {
        // Reparse only when the configured list actually changed.
        if (ReferenceEquals(_cachedSource, source))
            return _cachedNetworks;

        lock (_gate)
        {
            if (ReferenceEquals(_cachedSource, source))
                return _cachedNetworks;

            var parsed = new List<IPNetwork>(source.Length);
            foreach (var raw in source)
            {
                if (TryParse(raw, out var net))
                    parsed.Add(net);
                else
                    _logger.LogError("Ignoring invalid AllowedIpRanges entry: '{Entry}'", raw);
            }
            _cachedNetworks = parsed;
            _cachedSource = source;
            return parsed;
        }
    }

    private static bool TryParse(string? raw, out IPNetwork network)
    {
        network = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var s = raw.Trim();

        if (!s.Contains('/'))
        {
            // Bare address -> /32 (IPv4) or /128 (IPv6).
            if (!IPAddress.TryParse(s, out var ip)) return false;
            int prefix = ip.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
            network = new IPNetwork(ip, prefix);
            return true;
        }

        return IPNetwork.TryParse(s, out network);
    }
}

public static class IpAllowListMiddlewareExtensions
{
    public static IApplicationBuilder UseIpAllowList(this IApplicationBuilder app)
        => app.UseMiddleware<IpAllowListMiddleware>();
}
