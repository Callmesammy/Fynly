namespace TestFynly.Services;

/// <summary>
/// Unit tests for JwtTokenService - token generation and password hashing
/// </summary>
public class JwtTokenServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _service = new JwtTokenService(_mockConfiguration.Object);
    }

    [Fact]
    public void GenerateAccessToken_WithValidInput_ReturnsBase64EncodedToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var token = _service.GenerateAccessToken(userId, tenantId, email);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        // Verify it can be decoded
        var decodedBytes = Convert.FromBase64String(token);
        var decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);
        
        decodedString.Should().Contain(userId.ToString());
        decodedString.Should().Contain(tenantId.ToString());
        decodedString.Should().Contain(email);
    }

    [Fact]
    public void GenerateAccessToken_WithCustomExpiry_IncludesExpirationTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var email = "test@example.com";
        var expiresIn = TimeSpan.FromMinutes(30);

        // Act
        var beforeCall = DateTime.UtcNow;
        var token = _service.GenerateAccessToken(userId, tenantId, email, expiresIn);
        var afterCall = DateTime.UtcNow;

        // Assert
        var decodedBytes = Convert.FromBase64String(token);
        var decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);

        // Token should contain valid ISO date format (YYYY-MM-DDTHH:MM:SS...Z)
        decodedString.Should().Contain("T");
        decodedString.Should().Contain("Z");
        decodedString.Should().Contain(":"); // Contains time separators
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64EncodedRandomBytes()
    {
        // Act
        var token1 = _service.GenerateRefreshToken();
        var token2 = _service.GenerateRefreshToken();

        // Assert
        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();

        // Each call should generate a different token
        token1.Should().NotBe(token2);

        // Verify they're valid base64
        var decoded1 = Convert.FromBase64String(token1);
        var decoded2 = Convert.FromBase64String(token2);

        decoded1.Length.Should().Be(32);
        decoded2.Length.Should().Be(32);
    }

    [Fact]
    public void HashPassword_WithPassword_ReturnsValidHash()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hash = _service.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        
        // Hash should be base64 decodable
        var decodedHash = Convert.FromBase64String(hash);
        decodedHash.Should().NotBeEmpty();
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hash = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var password = "SecurePassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_SamePasswordDifferentHashes_VerifyBothWork()
    {
        // Arrange
        var password = "SamePassword123!";

        // Act
        var hash1 = _service.HashPassword(password);
        var hash2 = _service.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // Different salts = different hashes
        
        _service.VerifyPassword(password, hash1).Should().BeTrue();
        _service.VerifyPassword(password, hash2).Should().BeTrue();
    }

    [Fact]
    public void HashRefreshToken_WithToken_ReturnsValidHash()
    {
        // Arrange
        var token = _service.GenerateRefreshToken();

        // Act
        var hash = _service.HashRefreshToken(token);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        var decodedHash = Convert.FromBase64String(hash);
        decodedHash.Length.Should().Be(32); // SHA256 = 32 bytes
    }

    [Fact]
    public void VerifyRefreshToken_WithCorrectToken_ReturnsTrue()
    {
        // Arrange
        var token = _service.GenerateRefreshToken();
        var hash = _service.HashRefreshToken(token);

        // Act
        var result = _service.VerifyRefreshToken(token, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyRefreshToken_WithWrongToken_ReturnsFalse()
    {
        // Arrange
        var token = _service.GenerateRefreshToken();
        var wrongToken = _service.GenerateRefreshToken();
        var hash = _service.HashRefreshToken(token);

        // Act
        var result = _service.VerifyRefreshToken(wrongToken, hash);

        // Assert
        result.Should().BeFalse();
    }
}
