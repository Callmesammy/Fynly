namespace AiCFO.Infrastructure.Services;

/// <summary>
/// Provides the current tenant context from HTTP request.
/// Injected as scoped service, resolves tenant per request.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    bool IsAuthenticated { get; }
    string? Email { get; }
}

/// <summary>
/// Implementation of ITenantContext.
/// Extracts tenant and user info from HttpContext.
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
                throw new InvalidOperationException("HttpContext is not available");

            var tenantIdClaim = httpContext.User.FindFirst("tenant_id");
            if (tenantIdClaim is null)
                throw new InvalidOperationException("Tenant ID claim not found in token");

            if (!Guid.TryParse(tenantIdClaim.Value, out var tenantId))
                throw new InvalidOperationException("Invalid tenant ID format");

            return tenantId;
        }
    }

    public Guid UserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
                throw new InvalidOperationException("HttpContext is not available");

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
