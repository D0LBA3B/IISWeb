using IISWeb.Models;
using IISWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IISWeb.Pages;

public class IndexModel : PageModel
{
    private readonly IIisPoolService _iis;
    private readonly IAuditService _audit;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IIisPoolService iis, IAuditService audit, ILogger<IndexModel> logger)
    {
        _iis = iis;
        _audit = audit;
        _logger = logger;
    }

    public IReadOnlyList<AppPoolInfo> Pools { get; private set; } = Array.Empty<AppPoolInfo>();
    public StatusMessage? Status { get; private set; }
    public string? LoadError { get; private set; }

    public record StatusMessage(string Title, string Message, bool Success);

    public void OnGet()
    {
        LoadPools();
        ConsumeTempStatus();
    }

    public Task<IActionResult> OnPostStartAsync(string? name)
        => DoAsync(AuditActions.Start, name, _iis.Start);

    public Task<IActionResult> OnPostStopAsync(string? name)
        => DoAsync(AuditActions.Stop, name, _iis.Stop);

    public Task<IActionResult> OnPostRecycleAsync(string? name)
        => DoAsync(AuditActions.Recycle, name, _iis.Recycle);

    private async Task<IActionResult> DoAsync(string action, string? name, Func<string, PoolActionResult> op)
    {
        if (!User.IsInRole(Roles.Admin))
        {
            await _audit.LogAsync(action, name, false, "Forbidden: caller is not Admin.");
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(name) || !_iis.IsAllowed(name))
        {
            await _audit.LogAsync(action, name, false, "Refused: invalid or disallowed pool name.");
            SetTempStatus(false, $"{action} refused", "Invalid or disallowed application pool name.");
            return RedirectToPage();
        }

        PoolActionResult result;
        try
        {
            result = op(name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during {Action} on pool {Pool}", action, name);
            await _audit.LogAsync(action, name, false, "Unexpected error: see server logs.");
            SetTempStatus(false, $"{action} failed", "An unexpected error occurred.");
            return RedirectToPage();
        }

        await _audit.LogAsync(action, name, result.Success, result.Message);
        SetTempStatus(result.Success, $"{action} {name}", result.Message);
        return RedirectToPage();
    }

    private void LoadPools()
    {
        try
        {
            Pools = _iis.List();
        }
        catch (UnauthorizedAccessException)
        {
            LoadError = "Access denied while reading IIS configuration. The Windows account hosting this app must be allowed to manage IIS.";
            Pools = Array.Empty<AppPoolInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate application pools");
            LoadError = "Could not read application pools. See server logs.";
            Pools = Array.Empty<AppPoolInfo>();
        }
    }

    private void SetTempStatus(bool success, string title, string message)
        => TempData["Status"] = $"{(success ? "ok" : "err")}|{title}|{message}";

    private void ConsumeTempStatus()
    {
        if (TempData["Status"] is string s)
        {
            var parts = s.Split('|', 3);
            if (parts.Length == 3)
                Status = new StatusMessage(parts[1], parts[2], parts[0] == "ok");
        }
    }
}
