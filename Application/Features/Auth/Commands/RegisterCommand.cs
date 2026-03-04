namespace AiCFO.Application.Features.Auth.Commands;

/// <summary>
/// Command to register a new user.
/// </summary>
public record RegisterCommand(
    Guid TenantId,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string PasswordConfirm) : IRequest<Result<AuthResponse>>;

/// <summary>
/// Handler for RegisterCommand.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private const int MinPasswordLength = 8;

    public RegisterCommandHandler(
        IAuthService authService,
        ITokenService tokenService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result<AuthResponse>.Fail("Email is required");

        if (!request.Email.Contains("@"))
            return Result<AuthResponse>.Fail("Invalid email format");

        if (string.IsNullOrWhiteSpace(request.Password))
            return Result<AuthResponse>.Fail("Password is required");

        if (request.Password.Length < MinPasswordLength)
            return Result<AuthResponse>.Fail($"Password must be at least {MinPasswordLength} characters");

        if (request.Password != request.PasswordConfirm)
            return Result<AuthResponse>.Fail("Passwords do not match");

        if (string.IsNullOrWhiteSpace(request.FirstName))
            return Result<AuthResponse>.Fail("First name is required");

        if (string.IsNullOrWhiteSpace(request.LastName))
            return Result<AuthResponse>.Fail("Last name is required");

        // Check if user already exists
        var existingUser = await _authService.GetUserByEmailAsync(request.TenantId, request.Email, cancellationToken);

        if (existingUser is not null)
            return Result<AuthResponse>.Fail("User with this email already exists");

        try
        {
            // Hash password
            var passwordHash = _tokenService.HashPassword(request.Password);

            // Create user entity
            var userId = Guid.NewGuid();
            var user = new User(
                id: userId,
                tenantId: request.TenantId,
                email: request.Email,
                firstName: request.FirstName,
                lastName: request.LastName,
                passwordHash: passwordHash,
                createdBy: userId);

            // Save to database
            var created = await _authService.CreateUserAsync(user, cancellationToken);
            if (!created)
                return Result<AuthResponse>.Fail("Failed to create user");

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(userId, request.TenantId, request.Email);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

            // Store refresh token
            user.SetRefreshToken(refreshTokenHash, refreshTokenExpiresAt, userId);
            await _authService.UpdateUserAsync(user, cancellationToken);

            var response = new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresIn: DateTime.UtcNow.AddMinutes(15),
                User: new UserDto(
                    Id: user.Id,
                    TenantId: user.TenantId,
                    Email: user.Email,
                    FirstName: user.FirstName,
                    LastName: user.LastName,
                    IsActive: user.IsActive));

            return Result<AuthResponse>.Ok(response);
        }
        catch
        {
            return Result<AuthResponse>.Fail("Registration failed. Please try again.");
        }
    }
}
