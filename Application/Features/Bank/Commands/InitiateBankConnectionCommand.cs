namespace AiCFO.Application.Features.Bank.Commands;

using Microsoft.Extensions.Logging;
using AiCFO.Application.Common;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Command to initiate a bank connection (OAuth2 flow).
/// </summary>
public class InitiateBankConnectionCommand : IRequest<Result<BankConnectionInitiationResponse>>
{
    public BankProvider Provider { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;

    public InitiateBankConnectionCommand(BankProvider provider, string bankCode, string bankName, string callbackUrl = "")
    {
        Provider = provider;
        BankCode = bankCode;
        BankName = bankName;
        CallbackUrl = callbackUrl;
    }
}

/// <summary>
/// Response containing connection ID and OAuth2 authorization URL.
/// </summary>
public class BankConnectionInitiationResponse
{
    public Guid ConnectionId { get; set; }
    public string AuthorizationUrl { get; set; } = string.Empty;
}

/// <summary>
/// Handler for initiating a bank connection.
/// Returns both the connection ID and the OAuth2 authorization URL.
/// </summary>
public class InitiateBankConnectionCommandHandler : IRequestHandler<InitiateBankConnectionCommand, Result<BankConnectionInitiationResponse>>
{
    private readonly IBankService _bankService;
    private readonly IBankProviderFactory _bankProviderFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<InitiateBankConnectionCommandHandler> _logger;

    public InitiateBankConnectionCommandHandler(
        IBankService bankService,
        IBankProviderFactory bankProviderFactory,
        ITenantContext tenantContext,
        ILogger<InitiateBankConnectionCommandHandler> logger)
    {
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _bankProviderFactory = bankProviderFactory ?? throw new ArgumentNullException(nameof(bankProviderFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<BankConnectionInitiationResponse>> Handle(InitiateBankConnectionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initiating bank connection for provider {Provider}", request.Provider);

            var bankCode = new BankCode(request.BankCode);

            // Create the bank connection (in Draft/Pending state)
            var connection = await _bankService.CreateBankConnectionAsync(
                _tenantContext.TenantId,
                request.Provider,
                bankCode,
                request.BankName,
                _tenantContext.UserId,
                cancellationToken);

            // Get the provider instance
            var provider = _bankProviderFactory.CreateProvider(request.Provider);

            // Generate OAuth2 authorization URL
            var authResult = await provider.GetAuthorizationUrlAsync(
                _tenantContext.TenantId,
                request.CallbackUrl,
                cancellationToken);

            _logger.LogInformation("Successfully generated OAuth URL for connection {ConnectionId}", connection.Id);

            return Result<BankConnectionInitiationResponse>.Ok(new BankConnectionInitiationResponse
            {
                ConnectionId = connection.Id,
                AuthorizationUrl = authResult.AuthorizationUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate bank connection for provider {Provider}", request.Provider);
            return Result<BankConnectionInitiationResponse>.Fail($"Failed to initiate bank connection: {ex.Message}");
        }
    }
}
