namespace AiCFO.Application.Features.PredictiveAlerts.Commands;

/// <summary>
/// CQRS command to create a new predictive threshold.
/// </summary>
public record CreateThresholdCommand(CreateThresholdRequest Request) : IRequest<Result<PredictiveThresholdDto>>;

/// <summary>
/// Handler for CreateThresholdCommand.
/// </summary>
public class CreateThresholdCommandHandler : IRequestHandler<CreateThresholdCommand, Result<PredictiveThresholdDto>>
{
    private readonly IPredictiveAlertService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateThresholdCommandHandler> _logger;

    public CreateThresholdCommandHandler(
        IPredictiveAlertService service,
        ITenantContext tenantContext,
        ILogger<CreateThresholdCommandHandler> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<PredictiveThresholdDto>> Handle(CreateThresholdCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Creating predictive threshold: {Name}", request.Request.Name);
            return await _service.CreateThresholdAsync(_tenantContext.TenantId, request.Request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating threshold");
            return Result<PredictiveThresholdDto>.Fail($"Error creating threshold: {ex.Message}");
        }
    }
}

/// <summary>
/// CQRS command to update an existing predictive threshold.
/// </summary>
public record UpdateThresholdCommand(Guid ThresholdId, UpdateThresholdRequest Request) : IRequest<Result<PredictiveThresholdDto>>;

/// <summary>
/// Handler for UpdateThresholdCommand.
/// </summary>
public class UpdateThresholdCommandHandler : IRequestHandler<UpdateThresholdCommand, Result<PredictiveThresholdDto>>
{
    private readonly IPredictiveAlertService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UpdateThresholdCommandHandler> _logger;

    public UpdateThresholdCommandHandler(
        IPredictiveAlertService service,
        ITenantContext tenantContext,
        ILogger<UpdateThresholdCommandHandler> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<PredictiveThresholdDto>> Handle(UpdateThresholdCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Updating predictive threshold: {ThresholdId}", request.ThresholdId);
            return await _service.UpdateThresholdAsync(_tenantContext.TenantId, request.ThresholdId, request.Request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating threshold");
            return Result<PredictiveThresholdDto>.Fail($"Error updating threshold: {ex.Message}");
        }
    }
}

/// <summary>
/// CQRS command to evaluate all active thresholds and generate alerts.
/// </summary>
public record EvaluateThresholdsCommand : IRequest<Result<List<PredictiveAlertDto>>>;

/// <summary>
/// Handler for EvaluateThresholdsCommand.
/// </summary>
public class EvaluateThresholdsCommandHandler : IRequestHandler<EvaluateThresholdsCommand, Result<List<PredictiveAlertDto>>>
{
    private readonly IPredictiveAlertService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<EvaluateThresholdsCommandHandler> _logger;

    public EvaluateThresholdsCommandHandler(
        IPredictiveAlertService service,
        ITenantContext tenantContext,
        ILogger<EvaluateThresholdsCommandHandler> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<List<PredictiveAlertDto>>> Handle(EvaluateThresholdsCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Evaluating predictive thresholds for tenant: {TenantId}", _tenantContext.TenantId);
            return await _service.EvaluateThresholdsAsync(_tenantContext.TenantId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating thresholds");
            return Result<List<PredictiveAlertDto>>.Fail($"Error evaluating thresholds: {ex.Message}");
        }
    }
}

/// <summary>
/// CQRS command to acknowledge an alert.
/// </summary>
public record AcknowledgeAlertCommand(Guid AlertId, string? Notes = null) : IRequest<Result<string>>;

/// <summary>
/// Handler for AcknowledgeAlertCommand.
/// </summary>
public class AcknowledgeAlertCommandHandler : IRequestHandler<AcknowledgeAlertCommand, Result<string>>
{
    private readonly IPredictiveAlertService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AcknowledgeAlertCommandHandler> _logger;

    public AcknowledgeAlertCommandHandler(
        IPredictiveAlertService service,
        ITenantContext tenantContext,
        ILogger<AcknowledgeAlertCommandHandler> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(AcknowledgeAlertCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Acknowledging alert: {AlertId}", request.AlertId);
            return await _service.AcknowledgeAlertAsync(_tenantContext.TenantId, request.AlertId, request.Notes, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert");
            return Result<string>.Fail($"Error acknowledging alert: {ex.Message}");
        }
    }
}

/// <summary>
/// CQRS command to resolve an alert.
/// </summary>
public record ResolveAlertCommand(Guid AlertId, string? Notes = null) : IRequest<Result<string>>;

/// <summary>
/// Handler for ResolveAlertCommand.
/// </summary>
public class ResolveAlertCommandHandler : IRequestHandler<ResolveAlertCommand, Result<string>>
{
    private readonly IPredictiveAlertService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ResolveAlertCommandHandler> _logger;

    public ResolveAlertCommandHandler(
        IPredictiveAlertService service,
        ITenantContext tenantContext,
        ILogger<ResolveAlertCommandHandler> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(ResolveAlertCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Resolving alert: {AlertId}", request.AlertId);
            return await _service.ResolveAlertAsync(_tenantContext.TenantId, request.AlertId, request.Notes, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert");
            return Result<string>.Fail($"Error resolving alert: {ex.Message}");
        }
    }
}
