namespace AiCFO.API.Middleware;

using System.Security.Claims;

/// <summary>
/// Middleware to validate tenant ID in requests and set it in HttpContext.
/// Runs early in the pipeline to ensure tenant is known before any handler runs.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip tenant validation for public endpoints
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Extract tenant ID from JWT claim if user is authenticated
        if (context.User?.Identity?.IsAuthenticated ?? false)
        {
            var tenantIdClaim = context.User.FindFirst("tenant_id");
            if (tenantIdClaim is null)
            {
                _logger.LogWarning("Request from authenticated user without tenant_id claim. User: {User}", 
                    context.User.FindFirst("sub")?.Value ?? "unknown");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Tenant ID is missing" });
                return;
            }

            if (!Guid.TryParse(tenantIdClaim.Value, out _))
            {
                _logger.LogWarning("Invalid tenant_id format in claim. Value: {Value}", tenantIdClaim.Value);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid tenant ID format" });
                return;
            }

            // Store tenant ID in HttpContext items for easy access
            context.Items["TenantId"] = tenantIdClaim.Value;
            _logger.LogInformation("Request from tenant: {TenantId}", tenantIdClaim.Value);
        }
        else
        {
            // For unauthenticated requests, tenant must be provided in header
            var tenantIdHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(tenantIdHeader) && !IsPublicEndpoint(context.Request.Path))
            {
                _logger.LogWarning("Unauthenticated request without tenant ID. Path: {Path}", 
                    context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Tenant ID is required" });
                return;
            }

            if (!string.IsNullOrEmpty(tenantIdHeader) && !Guid.TryParse(tenantIdHeader, out _))
            {
                _logger.LogWarning("Invalid tenant ID format in header. Value: {Value}", tenantIdHeader);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid tenant ID format" });
                return;
            }

            if (!string.IsNullOrEmpty(tenantIdHeader))
            {
                context.Items["TenantId"] = tenantIdHeader;
            }
        }

        await _next(context);
    }

    private static bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/health",
            "/api/auth/register",
            "/api/auth/login",
            "/api/auth/refresh",
            "/api/auth/forgot-password",
            "/api/auth/reset-password",
            "/swagger",
            "/scalar",
            "/api-docs"
        };

        var pathStr = path.Value ?? "";
        return publicPaths.Any(publicPath => pathStr.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase));
    }
}
