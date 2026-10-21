using System.Security.Claims;
using AccessRequestSystem.Api.DTOs.Requests;
using AccessRequestSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessRequestSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly IAccessRequestService _requestService;

    public RequestsController(IAccessRequestService requestService)
    {
        _requestService = requestService;
    }

    /// <summary>
    /// Submits a new privileged access request. Restricted to Requesters.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Requester")]
    [ProducesResponseType(typeof(AccessRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRequest([FromBody] CreateAccessRequestDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized();
        }

        var result = await _requestService.CreateRequestAsync(currentUserId.Value, dto);
        return CreatedAtAction(nameof(GetMyRequests), new { id = result.Id }, result);
    }

    /// <summary>
    /// Lists all access requests submitted by the logged-in user. Restricted to Requesters.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Requester")]
    [ProducesResponseType(typeof(IEnumerable<AccessRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRequests()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized();
        }

        var requests = await _requestService.GetMyRequestsAsync(currentUserId.Value);
        return Ok(requests);
    }

    /// <summary>
    /// Lists all pending access requests. Restricted to Approvers and Admins.
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Approver,Admin")]
    [ProducesResponseType(typeof(IEnumerable<AccessRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingRequests()
    {
        var pendingRequests = await _requestService.GetPendingRequestsAsync();
        return Ok(pendingRequests);
    }

    /// <summary>
    /// Approves an access request, calculates expiration, hashes the grant token, and returns the plain secret token ONCE.
    /// Restricted to Approver and Admin roles. Approvers cannot approve their own requests.
    /// </summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Approver,Admin")]
    [ProducesResponseType(typeof(ApproveRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveRequest([FromRoute] int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized();
        }

        var response = await _requestService.ApproveRequestAsync(id, currentUserId.Value);
        return Ok(response);
    }

    /// <summary>
    /// Denies an access request with an optional reason.
    /// Restricted to Approver and Admin roles. Approvers cannot deny their own requests.
    /// </summary>
    [HttpPost("{id:int}/deny")]
    [Authorize(Roles = "Approver,Admin")]
    [ProducesResponseType(typeof(AccessRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DenyRequest([FromRoute] int id, [FromBody] DenyRequestDto? dto)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized();
        }

        var response = await _requestService.DenyRequestAsync(id, currentUserId.Value, dto?.Reason);
        return Ok(response);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var userId) ? userId : null;
    }
}
