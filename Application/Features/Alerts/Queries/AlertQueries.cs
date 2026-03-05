namespace AiCFO.Application.Features.Alerts.Queries;

using AiCFO.Application.Common;
using AiCFO.Application.Features.Alerts.Dtos;
using AiCFO.Domain.Entities;

/// <summary>
/// Query to get an alert by ID.
/// </summary>
public record GetAlertQuery(
    Guid AlertId) : IRequest<Result<AlertDto>>;

/// <summary>
/// Handler for getting alert by ID.
/// </summary>
public class GetAlertQueryHandler : IRequestHandler<GetAlertQuery, Result<AlertDto>>
{
    private readonly IAlertService _alertService;
    private readonly ILogger<GetAlertQueryHandler> _logger;

    public GetAlertQueryHandler(
        IAlertService alertService,
        ILogger<GetAlertQueryHandler> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AlertDto>> Handle(GetAlertQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling GetAlertQuery for alert {AlertId}", request.AlertId);

            var result = await _alertService.GetAlertAsync(request.AlertId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Alert {AlertId} not found: {Error}", request.AlertId, result.Error);
                return Result<AlertDto>.Fail(result.Error);
            }

            var alert = result.Value;
            var dto = MapToDto(alert);

            return Result<AlertDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GetAlertQuery for alert {AlertId}", request.AlertId);
            return Result<AlertDto>.Fail($"Error retrieving alert: {ex.Message}");
        }
    }

    private static AlertDto MapToDto(AlertNotification alert) => new(
        alert.AlertId,
        alert.AnomalyReference,
        alert.Severity,
        alert.Status,
        alert.Message,
        alert.ConfidenceScore,
        alert.CreatedAt,
        alert.AcknowledgedAt,
        alert.ResolvedAt,
        alert.AggregatedAnomalies.Count);
}

/// <summary>
/// Query to get recent alerts.
/// </summary>
public record GetRecentAlertsQuery(
    int DaysBack = 7,
    AlertStatus? FilterStatus = null) : IRequest<Result<List<AlertDto>>>;

/// <summary>
/// Handler for getting recent alerts.
/// </summary>
public class GetRecentAlertsQueryHandler : IRequestHandler<GetRecentAlertsQuery, Result<List<AlertDto>>>
{
    private readonly IAlertService _alertService;
    private readonly ILogger<GetRecentAlertsQueryHandler> _logger;

    public GetRecentAlertsQueryHandler(
        IAlertService alertService,
        ILogger<GetRecentAlertsQueryHandler> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<List<AlertDto>>> Handle(GetRecentAlertsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Handling GetRecentAlertsQuery (days back: {DaysBack}, status filter: {FilterStatus})",
                request.DaysBack, request.FilterStatus);

            var result = await _alertService.GetRecentAlertsAsync(request.DaysBack, request.FilterStatus);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve recent alerts: {Error}", result.Error);
                return Result<List<AlertDto>>.Fail(result.Error);
            }

            var alerts = result.Value;
            var dtos = alerts.Select(MapToDto).ToList();

            _logger.LogInformation("Retrieved {Count} recent alerts", dtos.Count);
            return Result<List<AlertDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GetRecentAlertsQuery");
            return Result<List<AlertDto>>.Fail($"Error retrieving alerts: {ex.Message}");
        }
    }

    private static AlertDto MapToDto(AlertNotification alert) => new(
        alert.AlertId,
        alert.AnomalyReference,
        alert.Severity,
        alert.Status,
        alert.Message,
        alert.ConfidenceScore,
        alert.CreatedAt,
        alert.AcknowledgedAt,
        alert.ResolvedAt,
        alert.AggregatedAnomalies.Count);
}

/// <summary>
/// Query to get open (unresolved) alerts.
/// </summary>
public record GetOpenAlertsQuery() : IRequest<Result<List<AlertDto>>>;

/// <summary>
/// Handler for getting open alerts.
/// </summary>
public class GetOpenAlertsQueryHandler : IRequestHandler<GetOpenAlertsQuery, Result<List<AlertDto>>>
{
    private readonly IAlertService _alertService;
    private readonly ILogger<GetOpenAlertsQueryHandler> _logger;

    public GetOpenAlertsQueryHandler(
        IAlertService alertService,
        ILogger<GetOpenAlertsQueryHandler> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<List<AlertDto>>> Handle(GetOpenAlertsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling GetOpenAlertsQuery");

            var result = await _alertService.GetOpenAlertsAsync();

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve open alerts: {Error}", result.Error);
                return Result<List<AlertDto>>.Fail(result.Error);
            }

            var alerts = result.Value;
            var dtos = alerts.Select(MapToDto).ToList();

            _logger.LogInformation("Retrieved {Count} open alerts", dtos.Count);
            return Result<List<AlertDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GetOpenAlertsQuery");
            return Result<List<AlertDto>>.Fail($"Error retrieving alerts: {ex.Message}");
        }
    }

    private static AlertDto MapToDto(AlertNotification alert) => new(
        alert.AlertId,
        alert.AnomalyReference,
        alert.Severity,
        alert.Status,
        alert.Message,
        alert.ConfidenceScore,
        alert.CreatedAt,
        alert.AcknowledgedAt,
        alert.ResolvedAt,
        alert.AggregatedAnomalies.Count);
}

/// <summary>
/// Query to get alert statistics.
/// </summary>
public record GetAlertStatisticsQuery() : IRequest<Result<AlertStatisticsDto>>;

/// <summary>
/// Handler for getting alert statistics.
/// </summary>
public class GetAlertStatisticsQueryHandler : IRequestHandler<GetAlertStatisticsQuery, Result<AlertStatisticsDto>>
{
    private readonly IAlertService _alertService;
    private readonly ILogger<GetAlertStatisticsQueryHandler> _logger;

    public GetAlertStatisticsQueryHandler(
        IAlertService alertService,
        ILogger<GetAlertStatisticsQueryHandler> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AlertStatisticsDto>> Handle(GetAlertStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling GetAlertStatisticsQuery");

            var result = await _alertService.GetAlertStatisticsAsync();

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve alert statistics: {Error}", result.Error);
                return Result<AlertStatisticsDto>.Fail(result.Error);
            }

            _logger.LogInformation("Retrieved alert statistics: {Total} total alerts", result.Value.TotalAlerts);
            return Result<AlertStatisticsDto>.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GetAlertStatisticsQuery");
            return Result<AlertStatisticsDto>.Fail($"Error retrieving statistics: {ex.Message}");
        }
    }
}
