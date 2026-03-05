namespace AiCFO.Application.Features.Bank.Queries;

using AiCFO.Application.Common;

/// <summary>
/// DTO for bank connection.
/// </summary>
public class BankConnectionDto
{
    public Guid Id { get; set; }
    public int Provider { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? SyncError { get; set; }
}

/// <summary>
/// Query to get all bank connections for tenant.
/// </summary>
public class GetBankConnectionsQuery : IRequest<Result<List<BankConnectionDto>>>
{
}

/// <summary>
/// Handler for getting bank connections.
/// </summary>
public class GetBankConnectionsQueryHandler : IRequestHandler<GetBankConnectionsQuery, Result<List<BankConnectionDto>>>
{
    private readonly IBankService _bankService;
    private readonly ITenantContext _tenantContext;

    public GetBankConnectionsQueryHandler(IBankService bankService, ITenantContext tenantContext)
    {
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<List<BankConnectionDto>>> Handle(GetBankConnectionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var connections = await _bankService.GetBankConnectionsAsync(_tenantContext.TenantId, cancellationToken);

            var dtos = connections
                .Select(c => new BankConnectionDto
                {
                    Id = c.Id,
                    Provider = (int)c.Provider,
                    BankCode = c.BankCode.Value,
                    BankName = c.BankName,
                    Status = (int)c.Status,
                    LastSyncAt = c.LastSyncAt,
                    SyncError = c.SyncError
                })
                .ToList();

            return Result<List<BankConnectionDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<List<BankConnectionDto>>.Fail($"Failed to retrieve bank connections: {ex.Message}");
        }
    }
}
