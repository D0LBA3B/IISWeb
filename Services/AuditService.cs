using IISWeb.Data;
using IISWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace IISWeb.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<AuditService> _logger;

    public AuditService(AppDbContext db, IHttpContextAccessor http, ILogger<AuditService> logger)
    {
        _db = db;
        _http = http;
        _logger = logger;
    }

    public IQueryable<AuditLog> Query() =>
        _db.AuditLogs.AsNoTracking().OrderByDescending(a => a.TimestampUtc);

    public async Task LogAsync(string action, string? appPool, bool success, string? message = null, string? userNameOverride = null)
    {
        try
        {
            var ctx = _http.HttpContext;
            string? ip = ctx?.Connection.RemoteIpAddress?.ToString();
            string? user = userNameOverride ?? ctx?.User?.Identity?.Name;

            var entry = new AuditLog
            {
                TimestampUtc = DateTime.UtcNow,
                UserName = Truncate(user, 64),
                IpAddress = Truncate(ip, 64),
                Action = Truncate(action ?? "Unknown", 64)!,
                AppPool = Truncate(appPool, 128),
                Success = success,
                Message = Truncate(message, 512)
            };
            _db.AuditLogs.Add(entry);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "AUDIT user={User} ip={Ip} action={Action} pool={Pool} success={Success} msg={Message}",
                entry.UserName, entry.IpAddress, entry.Action, entry.AppPool, entry.Success, entry.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log entry.");
        }
    }

    private static string? Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? s : (s.Length > max ? s[..max] : s);
}
