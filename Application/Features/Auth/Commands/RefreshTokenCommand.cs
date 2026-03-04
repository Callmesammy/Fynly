namespace AiCFO.Application.Features.Auth.Commands;

/// <summary>
/// Command to refresh an access token using a refresh token.
/// </summary>
public record RefreshTokenCommand(
    Guid TenantId,
    string RefreshToken) : IRequest<Result<AuthResponse>>;

/// <summary>
/// Handler for RefreshTokenCommand.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly ITenantContext _tenantContext;

    public RefreshTokenCommandHandler(
        IAuthService authService,
        ITokenService tokenService,
        ITenantContext tenantContext)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result<AuthResponse>.Fail("Refresh token is required");

        try
        {
            // TODO: Find user by refresh token (needs querying by RefreshTokenHash)
            // For now, simplified: get current user from claims and validate token expiry

            // This is a simplified implementation - in production, you'd query users by refresh token hash
            return Result<AuthResponse>.Fail("Refresh token validation not yet implemented");
        }
        catch
        {
            return Result<AuthResponse>.Fail("Token refresh failed. Please log in again.");
        }
    }
}
