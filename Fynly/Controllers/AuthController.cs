namespace AiCFO.API.Controllers;

/// <summary>
/// Authentication API endpoints for user registration, login, and token refresh.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new user for a tenant.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantIdHeader, out var tenantId))
            return BadRequest(ApiResponse<object>.Failure("Invalid or missing tenant ID"));

        var command = new RegisterCommand(
            tenantId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Password,
            request.PasswordConfirm);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<AuthResponse>.Failure(result.Error));

        return CreatedAtAction(nameof(Register), ApiResponse<AuthResponse>.Ok(result.Value));
    }

    /// <summary>
    /// Logs in a user and returns access + refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantIdHeader, out var tenantId))
            return Unauthorized(ApiResponse<object>.Failure("Invalid or missing tenant ID"));

        var command = new LoginCommand(tenantId, request.Email, request.Password);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(ApiResponse<AuthResponse>.Failure(result.Error));

        return Ok(ApiResponse<AuthResponse>.Ok(result.Value));
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantIdHeader, out var tenantId))
            return Unauthorized(ApiResponse<object>.Failure("Invalid or missing tenant ID"));

        var command = new RefreshTokenCommand(tenantId, request.RefreshToken);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(ApiResponse<AuthResponse>.Failure(result.Error));

        return Ok(ApiResponse<AuthResponse>.Ok(result.Value));
    }

    /// <summary>
    /// Logs out a user (requires authentication).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        // In a real application, you might want to blacklist the token or perform other cleanup
        return Ok(ApiResponse<object>.Ok(new { message = "Logged out successfully" }));
    }
}
