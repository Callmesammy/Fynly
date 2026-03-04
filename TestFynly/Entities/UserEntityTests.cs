namespace TestFynly.Entities;

/// <summary>
/// Unit tests for User entity
/// </summary>
public class UserEntityTests
{
    [Fact]
    public void CreateUser_WithValidData_CreatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var email = "test@example.com";
        var firstName = "John";
        var lastName = "Doe";
        var passwordHash = "hashed_password_here";
        var createdBy = userId;

        // Act
        var user = new User(userId, tenantId, email, firstName, lastName, passwordHash, createdBy);

        // Assert
        user.Id.Should().Be(userId);
        user.TenantId.Should().Be(tenantId);
        user.Email.Should().Be(email);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.PasswordHash.Should().Be(passwordHash);
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public void GetFullName_ReturnsFirstAndLastName()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", 
            "John", "Doe", "hash", Guid.NewGuid());

        // Act
        var fullName = user.GetFullName();

        // Assert
        fullName.Should().Be("John Doe");
    }

    [Fact]
    public void UpdatePasswordHash_UpdatesPasswordHashAndTimestamp()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", 
            "John", "Doe", "old_hash", Guid.NewGuid());
        var newHash = "new_hash";
        var updatedBy = Guid.NewGuid();

        // Act
        System.Threading.Thread.Sleep(100); // Small delay to ensure time difference
        user.UpdatePasswordHash(newHash, updatedBy);

        // Assert
        user.PasswordHash.Should().Be(newHash);
        user.UpdatedBy.Should().Be(updatedBy);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetRefreshToken_SetsTokenHashAndExpiry()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", 
            "John", "Doe", "hash", Guid.NewGuid());
        var tokenHash = "refresh_token_hash";
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var updatedBy = Guid.NewGuid();

        // Act
        user.SetRefreshToken(tokenHash, expiresAt, updatedBy);

        // Assert
        user.RefreshTokenHash.Should().Be(tokenHash);
        user.RefreshTokenExpiresAt.Should().Be(expiresAt);
        user.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void ClearRefreshToken_ClearsTokenAndExpiry()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", 
            "John", "Doe", "hash", Guid.NewGuid());
        user.SetRefreshToken("token_hash", DateTime.UtcNow.AddDays(7), Guid.NewGuid());
        var updatedBy = Guid.NewGuid();

        // Act
        user.ClearRefreshToken(updatedBy);

        // Assert
        user.RefreshTokenHash.Should().BeNull();
        user.RefreshTokenExpiresAt.Should().BeNull();
        user.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", 
            "John", "Doe", "hash", Guid.NewGuid());
        var deactivatedBy = Guid.NewGuid();

        // Act
        user.Deactivate(deactivatedBy);

        // Assert
        user.IsActive.Should().BeFalse();
        user.UpdatedBy.Should().Be(deactivatedBy);
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, Guid.NewGuid(), "test@example.com", 
            "John", "Doe", "hash", userId);
        user.Deactivate(userId);
        var activatedBy = Guid.NewGuid();

        // Act
        user.Activate(activatedBy);

        // Assert
        user.IsActive.Should().BeTrue();
        user.UpdatedBy.Should().Be(activatedBy);
    }
}
