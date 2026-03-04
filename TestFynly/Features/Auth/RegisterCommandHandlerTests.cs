namespace TestFynly.Features.Auth;

/// <summary>
/// Unit tests for RegisterCommandHandler
/// </summary>
public class RegisterCommandHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockTokenService = new Mock<ITokenService>();
        _handler = new RegisterCommandHandler(_mockAuthService.Object, _mockTokenService.Object);
    }

    [Fact]
    public async Task Handle_WithValidInput_CreatesUserAndReturnsTokens()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new RegisterCommand(
            TenantId: tenantId,
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe",
            Password: "SecurePass123!",
            PasswordConfirm: "SecurePass123!");

        var cancellationToken = CancellationToken.None;

        _mockAuthService.Setup(x => x.GetUserByEmailAsync(tenantId, "test@example.com", cancellationToken))
            .ReturnsAsync((User?)null);

        _mockTokenService.Setup(x => x.HashPassword("SecurePass123!"))
            .Returns("hashed_password");

        _mockTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), tenantId, "test@example.com", null))
            .Returns("access_token");

        _mockTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _mockTokenService.Setup(x => x.HashRefreshToken("refresh_token"))
            .Returns("refresh_token_hash");

        _mockAuthService.Setup(x => x.CreateUserAsync(It.IsAny<User>(), cancellationToken))
            .ReturnsAsync(true);

        _mockAuthService.Setup(x => x.UpdateUserAsync(It.IsAny<User>(), cancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access_token");
        result.Value!.RefreshToken.Should().Be("refresh_token");
        result.Value!.User.Email.Should().Be("test@example.com");
        result.Value!.User.FirstName.Should().Be("John");
        result.Value!.User.LastName.Should().Be("Doe");

        _mockAuthService.Verify(x => x.CreateUserAsync(It.IsAny<User>(), cancellationToken), Times.Once);
        _mockAuthService.Verify(x => x.UpdateUserAsync(It.IsAny<User>(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyEmail_ReturnsFail()
    {
        // Arrange
        var command = new RegisterCommand(
            TenantId: Guid.NewGuid(),
            Email: "",
            FirstName: "John",
            LastName: "Doe",
            Password: "SecurePass123!",
            PasswordConfirm: "SecurePass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Email");
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ReturnsFail()
    {
        // Arrange
        var command = new RegisterCommand(
            TenantId: Guid.NewGuid(),
            Email: "invalid-email",
            FirstName: "John",
            LastName: "Doe",
            Password: "SecurePass123!",
            PasswordConfirm: "SecurePass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email");
    }

    [Fact]
    public async Task Handle_WithShortPassword_ReturnsFail()
    {
        // Arrange
        var command = new RegisterCommand(
            TenantId: Guid.NewGuid(),
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe",
            Password: "Short1!",
            PasswordConfirm: "Short1!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least 8");
    }

    [Fact]
    public async Task Handle_WithMismatchedPasswords_ReturnsFail()
    {
        // Arrange
        var command = new RegisterCommand(
            TenantId: Guid.NewGuid(),
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe",
            Password: "SecurePass123!",
            PasswordConfirm: "SecurePass456!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("do not match");
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ReturnsFail()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new RegisterCommand(
            TenantId: tenantId,
            Email: "existing@example.com",
            FirstName: "John",
            LastName: "Doe",
            Password: "SecurePass123!",
            PasswordConfirm: "SecurePass123!");

        var existingUser = new User(Guid.NewGuid(), tenantId, "existing@example.com", 
            "Jane", "Smith", "hash", Guid.NewGuid());

        _mockAuthService.Setup(x => x.GetUserByEmailAsync(tenantId, "existing@example.com", CancellationToken.None))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }
}
