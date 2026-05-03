using IISWeb.Services;
using Microsoft.EntityFrameworkCore;

namespace IISWeb.Data;

public static class DbInitializer
{
    public const string EnvUser = "IISWEB_INITIAL_ADMIN_USER";
    public const string EnvPass = "IISWEB_INITIAL_ADMIN_PASS";

    public static async Task InitializeAsync(IServiceProvider sp, ILogger logger)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (await db.Users.AnyAsync())
            return;

        var user = Environment.GetEnvironmentVariable(EnvUser);
        var pass = Environment.GetEnvironmentVariable(EnvPass);

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            logger.LogWarning(
                "No users in DB. To create the initial admin, either set {EnvUser}/{EnvPass} environment variables and restart, OR run: IISWeb.exe seed-admin --username <name> --password <pwd>",
                EnvUser, EnvPass);
            return;
        }

        try
        {
            var users = sp.GetRequiredService<IUserService>();
            await users.CreateAdminAsync(user, pass);
            logger.LogInformation("Initial admin '{User}' was created from environment variables.", user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed initial admin from environment variables.");
        }
    }
}
