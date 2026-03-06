namespace AiCFO.Application.Features.Auth.Commands;

/// <summary>
/// Command to log in a user.
/// </summary>
public record LoginCommand(
    Guid TenantId,
    string Email,
    string Password) : IRequest<Result<AuthResponse>>;

/// <summary>
/// Handler for LoginCommand.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IAuthService authService,
        ITokenService tokenService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result<AuthResponse>.Fail("Email is required");

        if (string.IsNullOrWhiteSpace(request.Password))
            return Result<AuthResponse>.Fail("Password is required");

        // Find user
        var user = await _authService.GetUserByEmailAsync(request.TenantId, request.Email, cancellationToken);

        if (user is null)
            return Result<AuthResponse>.Fail("Invalid email or password");

        if (!user.IsActive)
            return Result<AuthResponse>.Fail("User account is inactive");

        // Verify password
        if (!_tokenService.VerifyPassword(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Fail("Invalid email or password");

        try
        {
            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.TenantId, user.Email);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

            // Store refresh token
            user.SetRefreshToken(refreshTokenHash, refreshTokenExpiresAt, user.Id);
            await _authService.UpdateUserAsync(user, cancellationToken);

            var response = new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresIn: 900,  // 15 minutes in seconds
                User: new AuthUserDto(
                    Id: user.Id.ToString(),
                    Email: user.Email,
                    Name: $"{user.FirstName} {user.LastName}",
                    TenantId: user.TenantId.ToString()));

            return Result<AuthResponse>.Ok(response);
        }
        catch
        {
            return Result<AuthResponse>.Fail("Login failed. Please try again.");
        }
    }
}
