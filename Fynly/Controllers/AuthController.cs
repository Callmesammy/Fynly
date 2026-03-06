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
        // Use tenant ID from header, or generate a new one if not provided
        Guid tenantId;
        if (string.IsNullOrEmpty(tenantIdHeader))
        {
            tenantId = Guid.NewGuid();
        }
        else if (!Guid.TryParse(tenantIdHeader, out tenantId))
        {
            return BadRequest(ApiResponse<object>.Failure("Invalid tenant ID format"));
        }

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
        // Tenant ID is optional for login (will be determined by email lookup)
        // But if provided, use it to scope the user lookup to that tenant
        Guid tenantId = Guid.Empty;
        if (!string.IsNullOrEmpty(tenantIdHeader) && !Guid.TryParse(tenantIdHeader, out tenantId))
            return Unauthorized(ApiResponse<object>.Failure("Invalid tenant ID format"));

        var command = new LoginCommand(tenantId == Guid.Empty ? Guid.NewGuid() : tenantId, request.Email, request.Password);
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
        // Tenant ID is optional (if provided in header)
        Guid tenantId = Guid.Empty;
        if (!string.IsNullOrEmpty(tenantIdHeader) && !Guid.TryParse(tenantIdHeader, out tenantId))
            return Unauthorized(ApiResponse<object>.Failure("Invalid tenant ID format"));

        var command = new RefreshTokenCommand(tenantId == Guid.Empty ? Guid.NewGuid() : tenantId, request.RefreshToken);
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
