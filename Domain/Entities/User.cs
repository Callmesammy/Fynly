namespace AiCFO.Domain.Entities;

/// <summary>
/// User entity representing a system user within a tenant.
/// Inherits from AggregateRoot for domain event support.
/// </summary>
public class User : AggregateRoot
{
    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string PasswordHash { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? RefreshTokenHash { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }

    // For EF Core
    private User() { }

    /// <summary>
    /// Creates a new user instance.
    /// </summary>
    public User(
        Guid id,
        Guid tenantId,
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        Guid createdBy)
        : base(id, tenantId, createdBy)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        IsActive = true;
    }

    /// <summary>
    /// Updates the password hash for the user.
    /// </summary>
    public void UpdatePasswordHash(string passwordHash, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Sets the refresh token for the user.
    /// </summary>
    public void SetRefreshToken(string refreshTokenHash, DateTime expiresAt, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenHash))
            throw new ArgumentException("Refresh token hash cannot be empty", nameof(refreshTokenHash));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Refresh token expiration must be in the future", nameof(expiresAt));

        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiresAt = expiresAt;
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Clears the refresh token for the user (logout).
    /// </summary>
    public void ClearRefreshToken(Guid updatedBy)
    {
        RefreshTokenHash = null;
        RefreshTokenExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Deactivates the user account.
    /// </summary>
    public void Deactivate(Guid updatedBy)
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Activates the user account.
    /// </summary>
    public void Activate(Guid updatedBy)
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Gets the full name of the user.
    /// </summary>
    public string GetFullName() => $"{FirstName} {LastName}";
}
