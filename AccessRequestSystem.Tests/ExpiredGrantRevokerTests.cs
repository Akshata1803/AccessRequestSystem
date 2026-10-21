using AccessRequestSystem.Data.Entities;
using AccessRequestSystem.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccessRequestSystem.Tests;

public class ExpiredGrantRevokerTests
{
    [Fact]
    public async Task RevokeExpiredGrants_TransitionsExpiredToRevoked_AndLeavesNonExpiredApproved()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var factory = new TestDbContextHelper.TestDbContextFactory(dbName);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Worker:MaxDegreeOfParallelism"] = "2"
            })
            .Build();

        var logger = NullLogger<ExpiredGrantRevoker>.Instance;

        // Seed initial data
        await using (var seedContext = factory.CreateDbContext())
        {
            var user = new User { Id = 1, Username = "testuser", PasswordHash = "hash", RoleId = 1 };
            var resource = new Resource { Id = 1, Name = "Target System" };

            // 1. Expired grant (expired 10 minutes ago)
            var expiredRequest = new AccessRequest
            {
                Id = 10,
                RequesterId = user.Id,
                ResourceId = resource.Id,
                Justification = "Expired test",
                DurationMinutes = 60,
                Status = AccessRequestStatus.Approved,
                CreatedAt = DateTime.UtcNow.AddMinutes(-70),
                DecidedAt = DateTime.UtcNow.AddMinutes(-70),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-10), // SURPASSED EXPIRATION
                GrantTokenHash = "some_secret_hash_value"
            };

            // 2. Active grant (expires in 45 minutes)
            var activeRequest = new AccessRequest
            {
                Id = 20,
                RequesterId = user.Id,
                ResourceId = resource.Id,
                Justification = "Active test",
                DurationMinutes = 60,
                Status = AccessRequestStatus.Approved,
                CreatedAt = DateTime.UtcNow.AddMinutes(-15),
                DecidedAt = DateTime.UtcNow.AddMinutes(-15),
                ExpiresAt = DateTime.UtcNow.AddMinutes(45), // STILL ACTIVE
                GrantTokenHash = "valid_active_hash_value"
            };

            seedContext.Users.Add(user);
            seedContext.Resources.Add(resource);
            seedContext.AccessRequests.AddRange(expiredRequest, activeRequest);
            await seedContext.SaveChangesAsync();
        }

        var revoker = new ExpiredGrantRevoker(factory, config, logger);

        // Act
        var revokedCount = await revoker.RevokeExpiredGrantsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, revokedCount);

        await using (var verifyContext = factory.CreateDbContext())
        {
            // Verify expired request was updated
            var updatedExpired = await verifyContext.AccessRequests.FindAsync(10);
            Assert.NotNull(updatedExpired);
            Assert.Equal(AccessRequestStatus.Expired, updatedExpired.Status);
            Assert.Null(updatedExpired.GrantTokenHash); // Token hash must be cleared

            // Verify active request was untouched
            var updatedActive = await verifyContext.AccessRequests.FindAsync(20);
            Assert.NotNull(updatedActive);
            Assert.Equal(AccessRequestStatus.Approved, updatedActive.Status);
            Assert.NotNull(updatedActive.GrantTokenHash); // Token hash retained

            // Verify system audit log was generated
            var auditLog = await verifyContext.AuditLogs
                .FirstOrDefaultAsync(l => l.EntityId == 10 && l.Action == "GrantExpired");

            Assert.NotNull(auditLog);
            Assert.Null(auditLog.ActorUserId); // System event has null actor
            Assert.Equal("AccessRequest", auditLog.EntityType);
        }
    }
}
