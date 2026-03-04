namespace AiCFO.Infrastructure.Services;

using AiCFO.Infrastructure.Persistence;

/// <summary>
/// Authentication service implementation using EF Core.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;

    public AuthService(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<User?> GetUserByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.TenantId == tenantId, cancellationToken);
    }

    public async Task<bool> CreateUserAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateUserAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
