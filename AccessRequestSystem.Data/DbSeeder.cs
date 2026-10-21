using AccessRequestSystem.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AccessRequestSystem.Data;

/// <summary>
/// Seeds essential lookup data and default development users.
/// Passwords are securely hashed with PasswordHasher<User>.
/// </summary>
public static class DbSeeder
{
    public const string DefaultDevPassword = "Password123!";

    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Seed Roles if none exist
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new() { Name = "Admin" },
                new() { Name = "Approver" },
                new() { Name = "Requester" }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        // Fetch role IDs
        var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
        var approverRole = await context.Roles.FirstAsync(r => r.Name == "Approver");
        var requesterRole = await context.Roles.FirstAsync(r => r.Name == "Requester");

        // 2. Seed Users (one per role) if none exist
        if (!await context.Users.AnyAsync())
        {
            var passwordHasher = new PasswordHasher<User>();

            var adminUser = new User
            {
                Username = "admin",
                RoleId = adminRole.Id
            };
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, DefaultDevPassword);

            var approverUser = new User
            {
                Username = "approver",
                RoleId = approverRole.Id
            };
            approverUser.PasswordHash = passwordHasher.HashPassword(approverUser, DefaultDevPassword);

            var requesterUser = new User
            {
                Username = "requester",
                RoleId = requesterRole.Id
            };
            requesterUser.PasswordHash = passwordHasher.HashPassword(requesterUser, DefaultDevPassword);

            await context.Users.AddRangeAsync(adminUser, approverUser, requesterUser);
            await context.SaveChangesAsync();
        }

        // 3. Seed Sample Resources if none exist
        if (!await context.Resources.AnyAsync())
        {
            var resources = new List<Resource>
            {
                new() { Name = "Production Database (Read-Only)" },
                new() { Name = "AWS Production Console" },
                new() { Name = "Billing Administration Portal" }
            };

            await context.Resources.AddRangeAsync(resources);
            await context.SaveChangesAsync();
        }
    }
}
