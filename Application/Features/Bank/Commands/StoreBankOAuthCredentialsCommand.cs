namespace AiCFO.Application.Features.Bank.Commands;

using AiCFO.Application.Common;

/// <summary>
/// Command to store OAuth2 credentials after bank authorization.
/// </summary>
public class StoreBankOAuthCredentialsCommand : IRequest<Result<bool>>
{
    public Guid ConnectionId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? RefreshToken { get; set; }

    public StoreBankOAuthCredentialsCommand(Guid connectionId, string accessToken, DateTime expiresAt, string? refreshToken = null)
    {
        ConnectionId = connectionId;
        AccessToken = accessToken;
        ExpiresAt = expiresAt;
        RefreshToken = refreshToken;
    }
}

/// <summary>
/// Handler for storing OAuth2 credentials.
/// </summary>
public class StoreBankOAuthCredentialsCommandHandler : IRequestHandler<StoreBankOAuthCredentialsCommand, Result<bool>>
{
    private readonly IBankService _bankService;
    private readonly ITenantContext _tenantContext;

    public StoreBankOAuthCredentialsCommandHandler(IBankService bankService, ITenantContext tenantContext)
    {
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<bool>> Handle(StoreBankOAuthCredentialsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _bankService.SetOAuthCredentialsAsync(
                _tenantContext.TenantId,
                request.ConnectionId,
                request.AccessToken,
                request.ExpiresAt,
                request.RefreshToken,
                _tenantContext.UserId,
                cancellationToken);

            return success
                ? Result<bool>.Ok(true)
                : Result<bool>.Fail("Failed to store OAuth credentials");
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Error storing OAuth credentials: {ex.Message}");
        }
    }
}
