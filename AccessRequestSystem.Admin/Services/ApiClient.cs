using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AccessRequestSystem.Admin.Models;

namespace AccessRequestSystem.Admin.Services;

public class ApiException : Exception
{
    public int StatusCode { get; }
    public string? ProblemDetail { get; }

    public ApiException(int statusCode, string message, string? problemDetail = null)
        : base(message)
    {
        StatusCode = statusCode;
        ProblemDetail = problemDetail;
    }
}

/// <summary>
/// Dedicated HTTP client for communicating with the AccessRequestSystem.Api.
/// Handles authentication state, JWT headers, and structured error handling.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public string? CurrentToken { get; private set; }
    public string? CurrentUsername { get; private set; }
    public string? CurrentRole { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken);

    public ApiClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');

        // Allow local dev self-signed HTTPS certificate
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_baseUrl)
        };
    }

    /// <summary>
    /// Authenticates with the API and stores the JWT in memory.
    /// </summary>
    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", new
            {
                username = username.Trim(),
                password
            });

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions)
                ?? throw new ApiException(500, "Received empty response from authentication server.");

            CurrentToken = result.Token;
            CurrentUsername = result.Username;
            CurrentRole = result.Role;

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", CurrentToken);

            return result;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(0, $"Unable to connect to API server at {_baseUrl}. Please verify the API is running.\n({ex.Message})");
        }
    }

    /// <summary>
    /// Fetches all pending access requests.
    /// </summary>
    public async Task<List<PendingRequestItem>> GetPendingRequestsAsync()
    {
        EnsureAuthenticated();
        try
        {
            var response = await _httpClient.GetAsync("/api/requests/pending");
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
            }

            return await response.Content.ReadFromJsonAsync<List<PendingRequestItem>>(JsonOptions)
                ?? new List<PendingRequestItem>();
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(0, $"Network error while loading pending requests: {ex.Message}");
        }
    }

    /// <summary>
    /// Approves an access request and returns the single-use secret token.
    /// </summary>
    public async Task<ApproveResponse> ApproveRequestAsync(int requestId)
    {
        EnsureAuthenticated();
        try
        {
            var response = await _httpClient.PostAsync($"/api/requests/{requestId}/approve", null);
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
            }

            return await response.Content.ReadFromJsonAsync<ApproveResponse>(JsonOptions)
                ?? throw new ApiException(500, "Received empty response for approval.");
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(0, $"Network error while approving request: {ex.Message}");
        }
    }

    /// <summary>
    /// Denies an access request with an optional justification.
    /// </summary>
    public async Task DenyRequestAsync(int requestId, string? reason)
    {
        EnsureAuthenticated();
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/requests/{requestId}/deny", new
            {
                reason = reason?.Trim()
            });

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
            }
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(0, $"Network error while denying request: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetches audit logs (Admin only).
    /// </summary>
    public async Task<List<AuditLogItem>> GetAuditLogsAsync(int page = 1, int pageSize = 50)
    {
        EnsureAuthenticated();
        try
        {
            var response = await _httpClient.GetAsync($"/api/audit?page={page}&pageSize={pageSize}");
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
            }

            var pagedResult = await response.Content.ReadFromJsonAsync<PagedAuditResult>(JsonOptions);
            return pagedResult?.Items ?? new List<AuditLogItem>();
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(0, $"Network error while loading audit logs: {ex.Message}");
        }
    }

    private void EnsureAuthenticated()
    {
        if (!IsAuthenticated)
        {
            throw new ApiException(401, "You are not authenticated. Please log in first.");
        }
    }

    private static async Task HandleErrorResponseAsync(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        string detail = string.Empty;

        try
        {
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("detail", out var detailElem))
            {
                detail = detailElem.GetString() ?? string.Empty;
            }
            else if (root.TryGetProperty("message", out var msgElem))
            {
                detail = msgElem.GetString() ?? string.Empty;
            }
            else if (root.TryGetProperty("title", out var titleElem))
            {
                detail = titleElem.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // Fallback if response is not JSON
        }

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Authentication failed: Invalid credentials or session expired.",
            HttpStatusCode.Forbidden => string.IsNullOrEmpty(detail)
                ? "Permission Denied: You do not have permission to perform this action (e.g. self-approval is forbidden)."
                : detail,
            HttpStatusCode.Conflict => string.IsNullOrEmpty(detail)
                ? "State Conflict: This request is not in 'Pending' status."
                : detail,
            HttpStatusCode.NotFound => string.IsNullOrEmpty(detail)
                ? "The requested item was not found."
                : detail,
            _ => string.IsNullOrEmpty(detail)
                ? $"API returned HTTP error {statusCode} ({response.ReasonPhrase})."
                : detail
        };

        throw new ApiException(statusCode, message, detail);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
