namespace AiCFO.Application.Features.Bank.Commands;

using Microsoft.Extensions.Logging;
using AiCFO.Application.Common;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Command to exchange OAuth2 authorization code for tokens.
/// This is called after the user authorizes at the bank.
/// </summary>
public class ExchangeOAuthCodeCommand : IRequest<Result<bool>>
{
    public Guid ConnectionId { get; set; }
    public string AuthorizationCode { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;

    public ExchangeOAuthCodeCommand(Guid connectionId, string authorizationCode, string callbackUrl)
    {
        ConnectionId = connectionId;
        AuthorizationCode = authorizationCode;
        CallbackUrl = callbackUrl;
    }
}

/// <summary>
/// Handler for exchanging OAuth2 authorization code for access tokens.
/// </summary>
public class ExchangeOAuthCodeCommandHandler : IRequestHandler<ExchangeOAuthCodeCommand, Result<bool>>
{
    private readonly IBankService _bankService;
    private readonly IBankProviderFactory _bankProviderFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ExchangeOAuthCodeCommandHandler> _logger;

    public ExchangeOAuthCodeCommandHandler(
        IBankService bankService,
        IBankProviderFactory bankProviderFactory,
        ITenantContext tenantContext,
        ILogger<ExchangeOAuthCodeCommandHandler> logger)
    {
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _bankProviderFactory = bankProviderFactory ?? throw new ArgumentNullException(nameof(bankProviderFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<bool>> Handle(ExchangeOAuthCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Exchanging OAuth code for connection {ConnectionId}", request.ConnectionId);

            // Get the bank connection to retrieve provider type
            var connection = await _bankService.GetBankConnectionAsync(
                _tenantContext.TenantId,
                request.ConnectionId,
                cancellationToken);

            if (connection == null)
                return Result<bool>.Fail("Bank connection not found");

            // Get the provider instance
            var provider = _bankProviderFactory.CreateProvider(connection.Provider);

            // Exchange authorization code for tokens
            var tokenResult = await provider.ExchangeCodeForTokenAsync(
                _tenantContext.TenantId,
                request.AuthorizationCode,
                cancellationToken);

            if (!tokenResult.Success)
                return Result<bool>.Fail($"Failed to exchange authorization code: {tokenResult.Error}");

            // Store the OAuth credentials
            var success = await _bankService.SetOAuthCredentialsAsync(
                _tenantContext.TenantId,
                request.ConnectionId,
                tokenResult.AccessToken,
                tokenResult.ExpiresAt,
                tokenResult.RefreshToken,
                _tenantContext.UserId,
                cancellationToken);

            if (!success)
                return Result<bool>.Fail("Failed to store OAuth credentials");

            _logger.LogInformation("Successfully exchanged OAuth code for connection {ConnectionId}", request.ConnectionId);
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging OAuth code for connection {ConnectionId}", request.ConnectionId);
            return Result<bool>.Fail($"Error exchanging OAuth code: {ex.Message}");
        }
    }
}
