namespace AiCFO.Application.Features.PredictiveAlerts.Queries;

/// <summary>
/// CQRS query to get all thresholds for a tenant.
/// </summary>
public record GetThresholdsQuery : IRequest<Result<List<PredictiveThresholdDto>>>;

/// <summary>
/// Handler for GetThresholdsQuery.
/// </summary>
public class GetThresholdsQueryHandler : IRequestHandler<GetThresholdsQuery, Result<List<PredictiveThresholdDto>>>
{
    private readonly IPredictiveAlertService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetThresholdsQueryHandler> _logger;

    public GetThresholdsQueryHandler(
        IPredictiveAlertService service,
        ITenantContext tenantContext,
        ILogger<GetThresholdsQueryHandler> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<List<PredictiveThresholdDto>>> Handle(GetThresholdsQuery request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting thresholds for tenant: {TenantId}", _tenantContext.TenantId);
            return await _service.GetThresholdsAsync(_tenantContext.TenantId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thresholds");
            return Result<List<PredictiveThresholdDto>>.Fail($"Error getting thresholds: {ex.Message}");
        }
    }
}

/// <summary>
/// CQRS query to get all alerts for a tenant.
/// </summary>
public record GetAlertsQuery : IRequest<Result<List<PredictiveAlertDto>>>;

/// <summary>
/// Handler for GetAlertsQuery.
/// </summary>
public class GetAlertsQueryHandler : IRequestHandler<GetAlertsQuery, Result<List<PredictiveAlertDto>>>
{
    private readonly IPredictiveAlertService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetAlertsQueryHandler> _logger;

    public GetAlertsQueryHandler(
        IPredictiveAlertService service,
        ITenantContext tenantContext,
        ILogger<GetAlertsQueryHandler> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<List<PredictiveAlertDto>>> Handle(GetAlertsQuery request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting alerts for tenant: {TenantId}", _tenantContext.TenantId);
            return await _service.GetAlertsAsync(_tenantContext.TenantId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts");
            return Result<List<PredictiveAlertDto>>.Fail($"Error getting alerts: {ex.Message}");
        }
    }
}

/// <summary>
/// CQRS query to get active alerts only.
/// </summary>
public record GetActiveAlertsQuery : IRequest<Result<List<PredictiveAlertDto>>>;

/// <summary>
/// Handler for GetActiveAlertsQuery.
/// </summary>
public class GetActiveAlertsQueryHandler : IRequestHandler<GetActiveAlertsQuery, Result<List<PredictiveAlertDto>>>
{
    private readonly IPredictiveAlertService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetActiveAlertsQueryHandler> _logger;

    public GetActiveAlertsQueryHandler(
        IPredictiveAlertService service,
        ITenantContext tenantContext,
        ILogger<GetActiveAlertsQueryHandler> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<List<PredictiveAlertDto>>> Handle(GetActiveAlertsQuery request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting active alerts for tenant: {TenantId}", _tenantContext.TenantId);
            return await _service.GetActiveAlertsAsync(_tenantContext.TenantId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts");
            return Result<List<PredictiveAlertDto>>.Fail($"Error getting active alerts: {ex.Message}");
        }
    }
}

/// <summary>
/// CQRS query to get statistics on thresholds and alerts.
/// </summary>
public record GetPredictiveAlertStatisticsQuery : IRequest<Result<PredictiveAlertStatisticsDto>>;

/// <summary>
/// Handler for GetPredictiveAlertStatisticsQuery.
/// </summary>
public class GetPredictiveAlertStatisticsQueryHandler : IRequestHandler<GetPredictiveAlertStatisticsQuery, Result<PredictiveAlertStatisticsDto>>
{
    private readonly IPredictiveAlertService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetPredictiveAlertStatisticsQueryHandler> _logger;

    public GetPredictiveAlertStatisticsQueryHandler(
        IPredictiveAlertService service,
        ITenantContext tenantContext,
        ILogger<GetPredictiveAlertStatisticsQueryHandler> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<PredictiveAlertStatisticsDto>> Handle(GetPredictiveAlertStatisticsQuery request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting predictive alert statistics for tenant: {TenantId}", _tenantContext.TenantId);
            return await _service.GetStatisticsAsync(_tenantContext.TenantId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return Result<PredictiveAlertStatisticsDto>.Fail($"Error getting statistics: {ex.Message}");
        }
    }
}

/// <summary>
/// CQRS query to get a single threshold by ID.
/// </summary>
public record GetThresholdQuery(Guid ThresholdId) : IRequest<Result<PredictiveThresholdDto>>;

/// <summary>
/// Handler for GetThresholdQuery.
/// </summary>
public class GetThresholdQueryHandler : IRequestHandler<GetThresholdQuery, Result<PredictiveThresholdDto>>
{
    private readonly IPredictiveAlertService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetThresholdQueryHandler> _logger;

    public GetThresholdQueryHandler(
        IPredictiveAlertService service,
        ITenantContext tenantContext,
        ILogger<GetThresholdQueryHandler> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<PredictiveThresholdDto>> Handle(GetThresholdQuery request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting threshold: {ThresholdId}", request.ThresholdId);
            return await _service.GetThresholdAsync(_tenantContext.TenantId, request.ThresholdId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting threshold");
            return Result<PredictiveThresholdDto>.Fail($"Error getting threshold: {ex.Message}");
        }
    }
}
