using AccessRequestSystem.Api.DTOs.Requests;

namespace AccessRequestSystem.Api.Services;

public interface IAccessRequestService
{
    Task<AccessRequestDto> CreateRequestAsync(int requesterId, CreateAccessRequestDto dto);
    Task<IReadOnlyList<AccessRequestDto>> GetMyRequestsAsync(int requesterId);
    Task<IReadOnlyList<AccessRequestDto>> GetPendingRequestsAsync();
    Task<ApproveRequestResponseDto> ApproveRequestAsync(int requestId, int approverId);
    Task<AccessRequestDto> DenyRequestAsync(int requestId, int approverId, string? reason);
}
