namespace AiCFO.Infrastructure.Services;

using AiCFO.Application.Common;

/// <summary>
/// Token service implementation using simple base64 encoding.
/// Production version should use System.IdentityModel.Tokens.Jwt for proper JWT signing.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private const int Pbkdf2Iterations = 10000;
    private const int SaltSize = 16;
    private const int DeriveKeySize = 20;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string GenerateAccessToken(Guid userId, Guid tenantId, string email, TimeSpan? expiresIn = null)
    {
        // Simplified token format: base64(userId|tenantId|email|expiresAt)
        // TODO: Upgrade to proper JWT signing with private key
        var expirationTime = expiresIn ?? TimeSpan.FromMinutes(15);
        var expiresAt = DateTime.UtcNow.Add(expirationTime);

        var tokenData = $"{userId}|{tenantId}|{email}|{expiresAt:O}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenData));
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }

        return Convert.ToBase64String(randomNumber);
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        // Generate random salt
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Derive key from password
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            DeriveKeySize);

        // Combine salt and hash for storage
        byte[] hashWithSalt = new byte[salt.Length + hash.Length];
        Buffer.BlockCopy(salt, 0, hashWithSalt, 0, salt.Length);
        Buffer.BlockCopy(hash, 0, hashWithSalt, salt.Length, hash.Length);

        return Convert.ToBase64String(hashWithSalt);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            // Extract salt and hash from stored value
            byte[] hashWithSalt = Convert.FromBase64String(hash);
            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(hashWithSalt, 0, salt, 0, SaltSize);

            // Derive key from provided password
            byte[] hashOfInput = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                DeriveKeySize);

            // Compare stored hash with computed hash
            byte[] storedHash = new byte[DeriveKeySize];
            Buffer.BlockCopy(hashWithSalt, SaltSize, storedHash, 0, DeriveKeySize);

            return hashOfInput.SequenceEqual(storedHash);
        }
        catch
        {
            return false;
        }
    }

    public string HashRefreshToken(string refreshToken)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public bool VerifyRefreshToken(string refreshToken, string refreshTokenHash)
    {
        var hashOfInput = HashRefreshToken(refreshToken);
        return hashOfInput == refreshTokenHash;
    }
}
