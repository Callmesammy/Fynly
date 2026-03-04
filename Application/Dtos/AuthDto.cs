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
/// DTO for authentication response (access + refresh token).
/// </summary>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresIn,
    UserDto User);

/// <summary>
/// DTO for user information in auth responses.
/// </summary>
public record UserDto(
    Guid Id,
    Guid TenantId,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive);
