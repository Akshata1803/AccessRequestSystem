using AccessRequestSystem.Api.DTOs.Audit;
using AccessRequestSystem.Api.DTOs.Common;
using AccessRequestSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessRequestSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Restricted strictly to system administrators
public class AuditController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Returns a paged, filterable audit log trail, ordered newest first. Restricted to Admins.
    /// </summary>
    /// <param name="action">Optional filter by action name (e.g. CreateRequest, ApproveRequest, DenyRequest).</param>
    /// <param name="actorUserId">Optional filter by user ID who triggered the action.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? action = null,
        [FromQuery] int? actorUserId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _auditLogService.GetAuditLogsAsync(action, actorUserId, page, pageSize);
        return Ok(result);
    }
}
