using System.ComponentModel.DataAnnotations;

namespace IISWeb.Models;

public class AppUser
{
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string Role { get; set; } = Roles.Admin;

    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutUntilUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginUtc { get; set; }
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Viewer = "Viewer";

    public static IReadOnlyList<string> All { get; } = new[] { Admin, Viewer };
}
