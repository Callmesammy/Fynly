namespace AiCFO.Application.Common;

/// <summary>
/// Service abstraction for token generation and password management.
/// Implementation in Infrastructure layer.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token for the given user.
    /// </summary>
    string GenerateAccessToken(Guid userId, Guid tenantId, string email, TimeSpan? expiresIn = null);

    /// <summary>
    /// Generates a refresh token (random bytes, base64 encoded).
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Hashes a password using PBKDF2+SHA256.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against its hash.
    /// </summary>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Hashes a refresh token using SHA256.
    /// </summary>
    string HashRefreshToken(string token);

    /// <summary>
    /// Verifies a refresh token against its hash.
    /// </summary>
    bool VerifyRefreshToken(string token, string hash);
}
