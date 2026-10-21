using AccessRequestSystem.Api.DTOs.Audit;
using AccessRequestSystem.Api.DTOs.Common;

namespace AccessRequestSystem.Api.Services;

public interface IAuditLogService
{
    Task LogAsync(int? actorUserId, string action, string entityType, int entityId, string details);
    Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(string? action, int? actorUserId, int page, int pageSize);
}
