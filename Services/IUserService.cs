using IISWeb.Models;

namespace IISWeb.Services;

public enum LoginResult
{
    Success,
    NotFound,
    InvalidPassword,
    LockedOut
}

public interface IUserService
{
    Task<AppUser?> FindByNameAsync(string userName);
    Task<(LoginResult Result, AppUser? User)> CheckPasswordAsync(string userName, string password);
    Task RegisterFailedAttemptAsync(AppUser user);
    Task ClearFailedAttemptsAsync(AppUser user);
    Task<AppUser> CreateAdminAsync(string userName, string password);
}
