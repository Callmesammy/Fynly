namespace AiCFO.Application.Common;

/// <summary>
/// Authentication service abstraction for Application layer.
/// Infrastructure implements this to provide database access.
/// </summary>
public interface IAuthService
{
    Task<User?> GetUserByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken);
    Task<bool> CreateUserAsync(User user, CancellationToken cancellationToken);
    Task<bool> UpdateUserAsync(User user, CancellationToken cancellationToken);
}
