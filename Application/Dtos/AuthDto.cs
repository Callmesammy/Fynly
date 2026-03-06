namespace AiCFO.Application.Dtos.Auth;

/// <summary>
/// DTO for user registration request.
/// </summary>
public record RegisterRequest(
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string PasswordConfirm);

/// <summary>
/// DTO for user login request.
/// </summary>
public record LoginRequest(
    string Email,
    string Password);

/// <summary>
/// DTO for refresh token request.
/// </summary>
public record RefreshTokenRequest(
    string RefreshToken);

/// <summary>
/// DTO for user information in auth responses (matches frontend expectations).
/// </summary>
public record AuthUserDto(
    string Id,
    string Email,
    string Name,           // Combined firstName + lastName
    string TenantId);

/// <summary>
/// DTO for authentication response (access + refresh token).
/// Matches frontend contract from audit specification.
/// </summary>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,        // Seconds (15 minutes = 900)
    AuthUserDto User);

/// <summary>
/// DTO for user information (legacy - kept for backward compatibility).
/// </summary>
public record UserDto(
    Guid Id,
    Guid TenantId,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive);
