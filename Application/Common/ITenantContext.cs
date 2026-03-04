namespace AiCFO.Application.Common;

/// <summary>
/// Service abstraction for accessing current tenant context.
/// Implementation in Infrastructure layer.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID from the request context.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Gets the current user ID from the JWT claims.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the current user's email from the JWT claims.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Indicates whether a valid tenant context has been established.
    /// </summary>
    bool IsAuthenticated { get; }
}
