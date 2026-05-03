using System.ComponentModel.DataAnnotations;

namespace IISWeb.Models;

public class AuditLog
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(64)] public string? UserName { get; set; }
    [MaxLength(64)] public string? IpAddress { get; set; }

    [Required, MaxLength(64)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(128)] public string? AppPool { get; set; }

    [Required] public bool Success { get; set; }

    [MaxLength(512)] public string? Message { get; set; }
}

public static class AuditActions
{
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string Start = "Start";
    public const string Stop = "Stop";
    public const string Recycle = "Recycle";
}
