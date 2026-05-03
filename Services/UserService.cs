using IISWeb.Configuration;
using IISWeb.Data;
using IISWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IISWeb.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;
    private readonly IOptionsMonitor<AppOptions> _opt;

    public UserService(AppDbContext db, IPasswordHasher<AppUser> hasher, IOptionsMonitor<AppOptions> opt)
    {
        _db = db;
        _hasher = hasher;
        _opt = opt;
    }

    public Task<AppUser?> FindByNameAsync(string userName)
        => _db.Users.SingleOrDefaultAsync(u => u.UserName == userName);

    public async Task<(LoginResult Result, AppUser? User)> CheckPasswordAsync(string userName, string password)
    {
        var user = await FindByNameAsync(userName);
        if (user is null)
            return (LoginResult.NotFound, null);

        if (user.LockoutUntilUtc is { } until && until > DateTime.UtcNow)
            return (LoginResult.LockedOut, user);

        var v = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (v == PasswordVerificationResult.Failed)
            return (LoginResult.InvalidPassword, user);

        if (v == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
            await _db.SaveChangesAsync();
        }

        return (LoginResult.Success, user);
    }

    public async Task RegisterFailedAttemptAsync(AppUser user)
    {
        var opt = _opt.CurrentValue;
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= opt.LoginMaxAttempts)
        {
            user.LockoutUntilUtc = DateTime.UtcNow.AddMinutes(Math.Max(1, opt.LoginLockoutMinutes));
            user.FailedLoginAttempts = 0;
        }
        await _db.SaveChangesAsync();
    }

    public async Task ClearFailedAttemptsAsync(AppUser user)
    {
        user.FailedLoginAttempts = 0;
        user.LockoutUntilUtc = null;
        user.LastLoginUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<AppUser> CreateAdminAsync(string userName, string password)
    {
        userName = (userName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("User name is required.", nameof(userName));
        if (userName.Length > 64)
            throw new ArgumentException("User name must be at most 64 characters.", nameof(userName));
        if (string.IsNullOrEmpty(password) || password.Length < 12)
            throw new ArgumentException("Password must be at least 12 characters long.", nameof(password));

        if (await _db.Users.AnyAsync(u => u.UserName == userName))
            throw new InvalidOperationException($"User '{userName}' already exists.");

        var u = new AppUser { UserName = userName, Role = Roles.Admin };
        u.PasswordHash = _hasher.HashPassword(u, password);
        _db.Users.Add(u);
        await _db.SaveChangesAsync();
        return u;
    }
}
