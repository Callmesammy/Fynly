namespace AiCFO.Application.Features.Alerts.Commands;

using AiCFO.Application.Common;
using AiCFO.Application.Features.Alerts.Dtos;
using AiCFO.Domain.Entities;

/// <summary>
/// Command to create a new alert notification.
/// </summary>
public record CreateAlertCommand(
    string AnomalyReference,
    AnomalySeverity Severity,
    decimal ConfidenceScore,
    string Message,
    int AutoDismissHours = 0) : IRequest<Result<AlertDto>>;

/// <summary>
/// Handler for creating alert notifications.
/// </summary>
public class CreateAlertCommandHandler : IRequestHandler<CreateAlertCommand, Result<AlertDto>>
{
    private readonly IAlertService _alertService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateAlertCommandHandler> _logger;

    public CreateAlertCommandHandler(
        IAlertService alertService,
        ITenantContext tenantContext,
        ILogger<CreateAlertCommandHandler> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AlertDto>> Handle(CreateAlertCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Handling CreateAlertCommand for anomaly {AnomalyReference} with severity {Severity}",
                request.AnomalyReference, request.Severity);

            var result = await _alertService.CreateAlertAsync(
                request.AnomalyReference,
                request.Severity,
                request.ConfidenceScore,
                request.Message,
                request.AutoDismissHours);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to create alert: {Error}", result.Error);
                return Result<AlertDto>.Fail(result.Error);
            }

            var alert = result.Value;
            var dto = MapToDto(alert);

            _logger.LogInformation("Successfully created alert {AlertId}", alert.AlertId);
            return Result<AlertDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling CreateAlertCommand for anomaly {AnomalyReference}", request.AnomalyReference);
            return Result<AlertDto>.Fail($"Error creating alert: {ex.Message}");
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
/// Command to acknowledge an alert.
/// </summary>
public record AcknowledgeAlertCommand(
    Guid AlertId,
    string AcknowledgedBy) : IRequest<Result<AlertDto>>;

/// <summary>
/// Handler for acknowledging alerts.
/// </summary>
public class AcknowledgeAlertCommandHandler : IRequestHandler<AcknowledgeAlertCommand, Result<AlertDto>>
{
    private readonly IAlertService _alertService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AcknowledgeAlertCommandHandler> _logger;

    public AcknowledgeAlertCommandHandler(
        IAlertService alertService,
        ITenantContext tenantContext,
        ILogger<AcknowledgeAlertCommandHandler> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AlertDto>> Handle(AcknowledgeAlertCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Handling AcknowledgeAlertCommand for alert {AlertId} by {AcknowledgedBy}",
                request.AlertId, request.AcknowledgedBy);

            var result = await _alertService.AcknowledgeAlertAsync(request.AlertId, request.AcknowledgedBy);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to acknowledge alert {AlertId}: {Error}", request.AlertId, result.Error);
                return Result<AlertDto>.Fail(result.Error);
            }

            var alert = result.Value;
            var dto = MapToDto(alert);

            _logger.LogInformation("Successfully acknowledged alert {AlertId}", request.AlertId);
            return Result<AlertDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling AcknowledgeAlertCommand for alert {AlertId}", request.AlertId);
            return Result<AlertDto>.Fail($"Error acknowledging alert: {ex.Message}");
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
/// Command to resolve an alert.
/// </summary>
public record ResolveAlertCommand(
    Guid AlertId,
    string ResolutionNotes) : IRequest<Result<AlertDto>>;

/// <summary>
/// Handler for resolving alerts.
/// </summary>
public class ResolveAlertCommandHandler : IRequestHandler<ResolveAlertCommand, Result<AlertDto>>
{
    private readonly IAlertService _alertService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ResolveAlertCommandHandler> _logger;

    public ResolveAlertCommandHandler(
        IAlertService alertService,
        ITenantContext tenantContext,
        ILogger<ResolveAlertCommandHandler> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AlertDto>> Handle(ResolveAlertCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Handling ResolveAlertCommand for alert {AlertId}",
                request.AlertId);

            var result = await _alertService.ResolveAlertAsync(request.AlertId, request.ResolutionNotes);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to resolve alert {AlertId}: {Error}", request.AlertId, result.Error);
                return Result<AlertDto>.Fail(result.Error);
            }

            var alert = result.Value;
            var dto = MapToDto(alert);

            _logger.LogInformation("Successfully resolved alert {AlertId}", request.AlertId);
            return Result<AlertDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ResolveAlertCommand for alert {AlertId}", request.AlertId);
            return Result<AlertDto>.Fail($"Error resolving alert: {ex.Message}");
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
/// Command to dismiss an alert.
/// </summary>
public record DismissAlertCommand(
    Guid AlertId) : IRequest<Result<AlertDto>>;

/// <summary>
/// Handler for dismissing alerts.
/// </summary>
public class DismissAlertCommandHandler : IRequestHandler<DismissAlertCommand, Result<AlertDto>>
{
    private readonly IAlertService _alertService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DismissAlertCommandHandler> _logger;

    public DismissAlertCommandHandler(
        IAlertService alertService,
        ITenantContext tenantContext,
        ILogger<DismissAlertCommandHandler> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AlertDto>> Handle(DismissAlertCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Handling DismissAlertCommand for alert {AlertId}",
                request.AlertId);

            var result = await _alertService.DismissAlertAsync(request.AlertId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to dismiss alert {AlertId}: {Error}", request.AlertId, result.Error);
                return Result<AlertDto>.Fail(result.Error);
            }

            var alert = result.Value;
            var dto = MapToDto(alert);

            _logger.LogInformation("Successfully dismissed alert {AlertId}", request.AlertId);
            return Result<AlertDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling DismissAlertCommand for alert {AlertId}", request.AlertId);
            return Result<AlertDto>.Fail($"Error dismissing alert: {ex.Message}");
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
