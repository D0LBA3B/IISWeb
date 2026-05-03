using IISWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace IISWeb.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.UserName).IsUnique();
        });

        b.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.TimestampUtc);
            e.HasIndex(a => a.UserName);
        });
    }
}
