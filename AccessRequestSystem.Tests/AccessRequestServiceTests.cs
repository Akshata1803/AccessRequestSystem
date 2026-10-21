using System.Security.Cryptography;
using System.Text;
using AccessRequestSystem.Api.Exceptions;
using AccessRequestSystem.Api.Services;
using AccessRequestSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessRequestSystem.Tests;

public class AccessRequestServiceTests
{
    [Fact]
    public async Task Approve_SetsExpiresAtCorrectly()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = TestDbContextHelper.CreateDbContext(dbName);

        var requester = new User { Id = 10, Username = "alice", PasswordHash = "hash", RoleId = 3 };
        var approver = new User { Id = 20, Username = "bob", PasswordHash = "hash", RoleId = 2 };
        var resource = new Resource { Id = 1, Name = "SQL Server Production" };

        var request = new AccessRequest
        {
            Id = 100,
            RequesterId = requester.Id,
            ResourceId = resource.Id,
            Justification = "Maintenance investigation",
            DurationMinutes = 60,
            Status = AccessRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(requester, approver);
        context.Resources.Add(resource);
        context.AccessRequests.Add(request);
        await context.SaveChangesAsync();

        var auditService = new AuditLogService(context);
        var service = new AccessRequestService(context, auditService);

        // Act
        var result = await service.ApproveRequestAsync(request.Id, approver.Id);

        // Assert
        Assert.Equal("Approved", result.Status);
        Assert.True(result.ExpiresAt > result.DecidedAt);
        var diff = result.ExpiresAt - result.DecidedAt;
        Assert.Equal(60, Math.Round(diff.TotalMinutes));

        var updated = await context.AccessRequests.FindAsync(request.Id);
        Assert.NotNull(updated);
        Assert.Equal(AccessRequestStatus.Approved, updated.Status);
        Assert.Equal(approver.Id, updated.DecidedById);
        Assert.NotNull(updated.ExpiresAt);
    }

    [Fact]
    public async Task Approve_StoresOnlyHash_NotPlainToken()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = TestDbContextHelper.CreateDbContext(dbName);

        var requester = new User { Id = 1, Username = "requester", PasswordHash = "hash", RoleId = 3 };
        var approver = new User { Id = 2, Username = "approver", PasswordHash = "hash", RoleId = 2 };
        var resource = new Resource { Id = 1, Name = "Billing Database" };

        var request = new AccessRequest
        {
            Id = 101,
            RequesterId = requester.Id,
            ResourceId = resource.Id,
            Justification = "Fix billing bug",
            DurationMinutes = 30,
            Status = AccessRequestStatus.Pending
        };

        context.Users.AddRange(requester, approver);
        context.Resources.Add(resource);
        context.AccessRequests.Add(request);
        await context.SaveChangesAsync();

        var service = new AccessRequestService(context, new AuditLogService(context));

        // Act
        var response = await service.ApproveRequestAsync(request.Id, approver.Id);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(response.GrantToken), "Plain token must be returned once in response.");

        var savedRequest = await context.AccessRequests.FindAsync(request.Id);
        Assert.NotNull(savedRequest);
        Assert.NotNull(savedRequest.GrantTokenHash);

        // Plain token must NEVER equal the stored hash
        Assert.NotEqual(response.GrantToken, savedRequest.GrantTokenHash);

        // The hash in the DB must be precisely SHA-256 of the returned plain token
        var computedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(response.GrantToken))).ToLowerInvariant();
        Assert.Equal(computedHash, savedRequest.GrantTokenHash);
    }

    [Fact]
    public async Task Approve_CannotApproveNonPendingRequest_ThrowsConflict()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = TestDbContextHelper.CreateDbContext(dbName);

        var requester = new User { Id = 1, Username = "user1", PasswordHash = "hash", RoleId = 3 };
        var approver = new User { Id = 2, Username = "user2", PasswordHash = "hash", RoleId = 2 };
        var resource = new Resource { Id = 1, Name = "Resource" };

        var request = new AccessRequest
        {
            Id = 102,
            RequesterId = requester.Id,
            ResourceId = resource.Id,
            Justification = "Test",
            DurationMinutes = 30,
            Status = AccessRequestStatus.Denied // Already decided!
        };

        context.Users.AddRange(requester, approver);
        context.Resources.Add(resource);
        context.AccessRequests.Add(request);
        await context.SaveChangesAsync();

        var service = new AccessRequestService(context, new AuditLogService(context));

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => service.ApproveRequestAsync(request.Id, approver.Id));
    }

    [Fact]
    public async Task Approve_CannotApproveOwnRequest_ThrowsForbidden()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = TestDbContextHelper.CreateDbContext(dbName);

        var approverUser = new User { Id = 5, Username = "approver_alice", PasswordHash = "hash", RoleId = 2 };
        var resource = new Resource { Id = 1, Name = "Resource" };

        // Alice requested access for herself
        var request = new AccessRequest
        {
            Id = 103,
            RequesterId = approverUser.Id,
            ResourceId = resource.Id,
            Justification = "Self-approval test",
            DurationMinutes = 45,
            Status = AccessRequestStatus.Pending
        };

        context.Users.Add(approverUser);
        context.Resources.Add(resource);
        context.AccessRequests.Add(request);
        await context.SaveChangesAsync();

        var service = new AccessRequestService(context, new AuditLogService(context));

        // Act & Assert: Approver attempting to approve their own request must throw ForbiddenException (403)
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.ApproveRequestAsync(request.Id, approverUser.Id));
        Assert.Contains("own", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deny_SetsStatusToDenied_AndDecidedFields()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = TestDbContextHelper.CreateDbContext(dbName);

        var requester = new User { Id = 1, Username = "requester", PasswordHash = "hash", RoleId = 3 };
        var approver = new User { Id = 2, Username = "approver", PasswordHash = "hash", RoleId = 2 };
        var resource = new Resource { Id = 1, Name = "Target Resource" };

        var request = new AccessRequest
        {
            Id = 104,
            RequesterId = requester.Id,
            ResourceId = resource.Id,
            Justification = "Need access",
            DurationMinutes = 60,
            Status = AccessRequestStatus.Pending
        };

        context.Users.AddRange(requester, approver);
        context.Resources.Add(resource);
        context.AccessRequests.Add(request);
        await context.SaveChangesAsync();

        var service = new AccessRequestService(context, new AuditLogService(context));

        // Act
        var result = await service.DenyRequestAsync(request.Id, approver.Id, "Access not justified for this role.");

        // Assert
        Assert.Equal("Denied", result.Status);
        Assert.Equal(approver.Id, result.DecidedById);
        Assert.NotNull(result.DecidedAt);

        var updated = await context.AccessRequests.FindAsync(request.Id);
        Assert.NotNull(updated);
        Assert.Equal(AccessRequestStatus.Denied, updated.Status);
    }

    [Fact]
    public async Task Approve_And_Deny_EachWriteAuditLog()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = TestDbContextHelper.CreateDbContext(dbName);

        var requester = new User { Id = 1, Username = "requester", PasswordHash = "hash", RoleId = 3 };
        var approver = new User { Id = 2, Username = "approver", PasswordHash = "hash", RoleId = 2 };
        var resource = new Resource { Id = 1, Name = "Resource" };

        var req1 = new AccessRequest { Id = 201, RequesterId = requester.Id, ResourceId = resource.Id, Justification = "J1", DurationMinutes = 30, Status = AccessRequestStatus.Pending };
        var req2 = new AccessRequest { Id = 202, RequesterId = requester.Id, ResourceId = resource.Id, Justification = "J2", DurationMinutes = 30, Status = AccessRequestStatus.Pending };

        context.Users.AddRange(requester, approver);
        context.Resources.Add(resource);
        context.AccessRequests.AddRange(req1, req2);
        await context.SaveChangesAsync();

        var service = new AccessRequestService(context, new AuditLogService(context));

        // Act
        await service.ApproveRequestAsync(req1.Id, approver.Id);
        await service.DenyRequestAsync(req2.Id, approver.Id, "Policy violation");

        // Assert: 2 AuditLogs rows must be present
        var auditLogs = await context.AuditLogs.OrderBy(l => l.Id).ToListAsync();
        Assert.Equal(2, auditLogs.Count);

        var approveLog = auditLogs.First(l => l.Action == "ApproveRequest");
        Assert.Equal(approver.Id, approveLog.ActorUserId);
        Assert.Equal(req1.Id, approveLog.EntityId);
        Assert.Equal("AccessRequest", approveLog.EntityType);

        var denyLog = auditLogs.First(l => l.Action == "DenyRequest");
        Assert.Equal(approver.Id, denyLog.ActorUserId);
        Assert.Equal(req2.Id, denyLog.EntityId);
        Assert.Contains("Policy violation", denyLog.Details);
    }
}
