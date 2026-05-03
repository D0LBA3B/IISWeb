using IISWeb.Models;

namespace IISWeb.Services;

public interface IAuditService
{
    Task LogAsync(string action, string? appPool, bool success, string? message = null, string? userNameOverride = null);
    IQueryable<AuditLog> Query();
}
