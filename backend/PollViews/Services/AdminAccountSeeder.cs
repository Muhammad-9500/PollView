using Microsoft.EntityFrameworkCore;
using PollViews.Data;
using PollViews.Entities;

namespace PollViews.Services;

public sealed class AdminAccountSeeder(
    PollViewsDbContext db,
    IConfiguration configuration,
    AdminPasswordService passwords,
    ILogger<AdminAccountSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var username = configuration["Admin:Username"]?.Trim();
        var password = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Admin:Username and Admin:Password must be configured.");

        var admin = await db.AdminUsers.FirstOrDefaultAsync(user => user.Username == username, cancellationToken);
        if (admin is null)
        {
            db.AdminUsers.Add(new AdminUser
            {
                Username = username,
                PasswordHash = passwords.Hash(password),
                MustChangePassword = true
            });
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded admin account {Username}.", username);
        }
    }
}
