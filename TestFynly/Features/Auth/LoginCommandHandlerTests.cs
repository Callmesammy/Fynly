namespace TestFynly.Features.Auth;

/// <summary>
/// Unit tests for LoginCommandHandler
/// </summary>
public class LoginCommandHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockTokenService = new Mock<ITokenService>();
        _handler = new LoginCommandHandler(_mockAuthService.Object, _mockTokenService.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new LoginCommand(
            TenantId: tenantId,
            Email: "test@example.com",
            Password: "SecurePass123!");

        var cancellationToken = CancellationToken.None;

        var user = new User(userId, tenantId, "test@example.com", 
            "John", "Doe", "hashed_password", userId);

        _mockAuthService.Setup(x => x.GetUserByEmailAsync(tenantId, "test@example.com", cancellationToken))
            .ReturnsAsync(user);

        _mockTokenService.Setup(x => x.VerifyPassword("SecurePass123!", "hashed_password"))
            .Returns(true);

        _mockTokenService.Setup(x => x.GenerateAccessToken(userId, tenantId, "test@example.com", null))
            .Returns("access_token");

        _mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _mockTokenService.Setup(x => x.HashRefreshToken("refresh_token"))
            .Returns("refresh_token_hash");

        _mockAuthService.Setup(x => x.UpdateUserAsync(It.IsAny<User>(), cancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access_token");
        result.Value!.RefreshToken.Should().Be("refresh_token");
        result.Value!.User.Id.Should().Be(userId.ToString());
    }

    [Fact]
    public async Task Handle_WithEmptyEmail_ReturnsFail()
    {
        // Arrange
        var command = new LoginCommand(
            TenantId: Guid.NewGuid(),
            Email: "",
            Password: "SecurePass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Email");
    }

    [Fact]
    public async Task Handle_WithEmptyPassword_ReturnsFail()
    {
        // Arrange
        var command = new LoginCommand(
            TenantId: Guid.NewGuid(),
            Email: "test@example.com",
            Password: "");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Password");
    }

    [Fact]
    public async Task Handle_WithNonexistentUser_ReturnsFail()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new LoginCommand(
            TenantId: tenantId,
            Email: "nonexistent@example.com",
            Password: "SecurePass123!");

        var cancellationToken = CancellationToken.None;

        _mockAuthService.Setup(x => x.GetUserByEmailAsync(tenantId, "nonexistent@example.com", cancellationToken))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ReturnsFail()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new LoginCommand(
            TenantId: tenantId,
            Email: "test@example.com",
            Password: "SecurePass123!");

        var cancellationToken = CancellationToken.None;

        var user = new User(userId, tenantId, "test@example.com", 
            "John", "Doe", "hashed_password", userId);
        user.Deactivate(userId);

        _mockAuthService.Setup(x => x.GetUserByEmailAsync(tenantId, "test@example.com", cancellationToken))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inactive");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsFail()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new LoginCommand(
            TenantId: tenantId,
            Email: "test@example.com",
            Password: "WrongPassword!");

        var cancellationToken = CancellationToken.None;

        var user = new User(userId, tenantId, "test@example.com", 
            "John", "Doe", "hashed_password", userId);

        _mockAuthService.Setup(x => x.GetUserByEmailAsync(tenantId, "test@example.com", cancellationToken))
            .ReturnsAsync(user);

        _mockTokenService.Setup(x => x.VerifyPassword("WrongPassword!", "hashed_password"))
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email or password");
    }
}
