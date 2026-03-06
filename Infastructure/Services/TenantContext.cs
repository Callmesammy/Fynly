namespace AiCFO.Infrastructure.Services;

using AiCFO.Application.Common;

/// <summary>
/// Implementation of ITenantContext.
/// Extracts tenant and user info from HttpContext JWT claims.
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Guid TenantId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
                return Guid.Empty;

            // JWT claim takes precedence
            var tenantIdClaim = httpContext.User.FindFirst("tenant_id");
            if (tenantIdClaim is not null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
                return tenantId;

            // Fallback to X-Tenant-Id header for public endpoints
            var tenantIdHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantIdHeader) && Guid.TryParse(tenantIdHeader, out var tenantIdFromHeader))
                return tenantIdFromHeader;

            // Return empty GUID for unauthenticated requests (e.g., registration)
            return Guid.Empty;
        }
    }

    public Guid UserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
                throw new InvalidOperationException("HttpContext is not available");

            // Try "sub" claim first (standard JWT), then "user_id" (custom)
            var userIdClaim = httpContext.User.FindFirst("sub") ?? httpContext.User.FindFirst("user_id");
            if (userIdClaim is null)
                throw new InvalidOperationException("User ID claim not found in token");

            if (!Guid.TryParse(userIdClaim.Value, out var userId))
                throw new InvalidOperationException("Invalid user ID format");

            return userId;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.User?.Identity?.IsAuthenticated ?? false;
        }
    }

    public string? Email
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
                return null;

            return httpContext.User.FindFirst("email")?.Value;
        }
    }
}
