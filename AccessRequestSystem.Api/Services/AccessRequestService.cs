using System.Security.Cryptography;
using System.Text;
using AccessRequestSystem.Api.DTOs.Requests;
using AccessRequestSystem.Api.Exceptions;
using AccessRequestSystem.Data;
using AccessRequestSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessRequestSystem.Api.Services;

/// <summary>
/// Business logic for creating, querying, approving, and denying access requests.
/// Enforces separation of duties and writes audit logs for every state transition.
/// </summary>
public class AccessRequestService : IAccessRequestService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public AccessRequestService(AppDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<AccessRequestDto> CreateRequestAsync(int requesterId, CreateAccessRequestDto dto)
    {
        var resource = await _context.Resources.FindAsync(dto.ResourceId)
            ?? throw new NotFoundException($"Resource with ID {dto.ResourceId} was not found.");

        var request = new AccessRequest
        {
            RequesterId = requesterId,
            ResourceId = dto.ResourceId,
            Justification = dto.Justification.Trim(),
            DurationMinutes = dto.DurationMinutes,
            Status = AccessRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.AccessRequests.Add(request);
        await _context.SaveChangesAsync();

        // Audit log creation
        await _auditLogService.LogAsync(
            actorUserId: requesterId,
            action: "CreateRequest",
            entityType: nameof(AccessRequest),
            entityId: request.Id,
            details: $"Created access request for resource '{resource.Name}' ({dto.DurationMinutes} minutes)."
        );

        var requester = await _context.Users.FindAsync(requesterId);

        return MapToDto(request, requester?.Username ?? "Unknown", resource.Name);
    }

    public async Task<IReadOnlyList<AccessRequestDto>> GetMyRequestsAsync(int requesterId)
    {
        return await _context.AccessRequests
            .Include(a => a.Requester)
            .Include(a => a.Resource)
            .Include(a => a.DecidedBy)
            .Where(a => a.RequesterId == requesterId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => MapToDto(a, a.Requester.Username, a.Resource.Name, a.DecidedBy != null ? a.DecidedBy.Username : null))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AccessRequestDto>> GetPendingRequestsAsync()
    {
        return await _context.AccessRequests
            .Include(a => a.Requester)
            .Include(a => a.Resource)
            .Where(a => a.Status == AccessRequestStatus.Pending)
            .OrderBy(a => a.CreatedAt)
            .Select(a => MapToDto(a, a.Requester.Username, a.Resource.Name, null))
            .ToListAsync();
    }

    public async Task<ApproveRequestResponseDto> ApproveRequestAsync(int requestId, int approverId)
    {
        var request = await _context.AccessRequests
            .Include(a => a.Resource)
            .FirstOrDefaultAsync(a => a.Id == requestId)
            ?? throw new NotFoundException($"Access request with ID {requestId} was not found.");

        // Rule: an approver cannot approve their own request
        if (request.RequesterId == approverId)
        {
            throw new ForbiddenException("Approvers are not permitted to approve their own access requests.");
        }

        // Rule: only allowed if status is Pending
        if (request.Status != AccessRequestStatus.Pending)
        {
            throw new ConflictException($"Access request cannot be approved because it is currently '{request.Status}', not 'Pending'.");
        }

        var now = DateTime.UtcNow;
        request.Status = AccessRequestStatus.Approved;
        request.DecidedById = approverId;
        request.DecidedAt = now;
        request.ExpiresAt = now.AddMinutes(request.DurationMinutes);

        // Generate a cryptographically secure random 256-bit secret token
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var plainGrantToken = Convert.ToHexString(randomBytes).ToLowerInvariant();

        // Store ONLY the SHA-256 hash in the database
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainGrantToken));
        request.GrantTokenHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        await _context.SaveChangesAsync();

        // Audit log approval
        await _auditLogService.LogAsync(
            actorUserId: approverId,
            action: "ApproveRequest",
            entityType: nameof(AccessRequest),
            entityId: request.Id,
            details: $"Approved access to '{request.Resource.Name}' for {request.DurationMinutes}m. Expires at {request.ExpiresAt:u}."
        );

        return new ApproveRequestResponseDto
        {
            RequestId = request.Id,
            Status = request.Status.ToString(),
            DecidedAt = request.DecidedAt.Value,
            ExpiresAt = request.ExpiresAt.Value,
            GrantToken = plainGrantToken
        };
    }

    public async Task<AccessRequestDto> DenyRequestAsync(int requestId, int approverId, string? reason)
    {
        var request = await _context.AccessRequests
            .Include(a => a.Requester)
            .Include(a => a.Resource)
            .FirstOrDefaultAsync(a => a.Id == requestId)
            ?? throw new NotFoundException($"Access request with ID {requestId} was not found.");

        // Rule: an approver cannot deny their own request
        if (request.RequesterId == approverId)
        {
            throw new ForbiddenException("Approvers are not permitted to deny their own access requests.");
        }

        // Rule: only allowed if status is Pending
        if (request.Status != AccessRequestStatus.Pending)
        {
            throw new ConflictException($"Access request cannot be denied because it is currently '{request.Status}', not 'Pending'.");
        }

        var now = DateTime.UtcNow;
        request.Status = AccessRequestStatus.Denied;
        request.DecidedById = approverId;
        request.DecidedAt = now;

        await _context.SaveChangesAsync();

        // Audit log denial
        var denialDetails = string.IsNullOrWhiteSpace(reason)
            ? $"Denied access request for '{request.Resource.Name}'."
            : $"Denied access request for '{request.Resource.Name}'. Reason: {reason.Trim()}";

        await _auditLogService.LogAsync(
            actorUserId: approverId,
            action: "DenyRequest",
            entityType: nameof(AccessRequest),
            entityId: request.Id,
            details: denialDetails
        );

        var approver = await _context.Users.FindAsync(approverId);

        return MapToDto(request, request.Requester.Username, request.Resource.Name, approver?.Username);
    }

    private static AccessRequestDto MapToDto(
        AccessRequest a,
        string requesterUsername,
        string resourceName,
        string? decidedByUsername = null)
    {
        return new AccessRequestDto
        {
            Id = a.Id,
            RequesterId = a.RequesterId,
            RequesterUsername = requesterUsername,
            ResourceId = a.ResourceId,
            ResourceName = resourceName,
            Justification = a.Justification,
            DurationMinutes = a.DurationMinutes,
            Status = a.Status.ToString(),
            CreatedAt = a.CreatedAt,
            DecidedById = a.DecidedById,
            DecidedByUsername = decidedByUsername,
            DecidedAt = a.DecidedAt,
            ExpiresAt = a.ExpiresAt
        };
    }
}
