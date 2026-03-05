namespace AiCFO.Application.Features.HealthReports.Commands;

using AiCFO.Application.Common;
using AiCFO.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Command to generate a new health report.
/// </summary>
public record GenerateHealthReportCommand(ReportType ReportType) : IRequest<Result<HealthReportDetailDto>>;

/// <summary>
/// Command to schedule a health report for recurring delivery.
/// </summary>
public record ScheduleHealthReportCommand(Guid ReportId, ScheduleReportRequest Request) 
    : IRequest<Result<HealthReportDetailDto>>;

/// <summary>
/// Command to send a health report to recipients.
/// </summary>
public record SendHealthReportCommand(Guid ReportId, string Recipients) 
    : IRequest<Result<bool>>;

/// <summary>
/// Command to archive a health report.
/// </summary>
public record ArchiveHealthReportCommand(Guid ReportId) 
    : IRequest<Result<bool>>;

/// <summary>
/// Handler for GenerateHealthReportCommand.
/// </summary>
public class GenerateHealthReportCommandHandler : IRequestHandler<GenerateHealthReportCommand, Result<HealthReportDetailDto>>
{
    private readonly IHealthReportService _healthReportService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GenerateHealthReportCommandHandler> _logger;

    public GenerateHealthReportCommandHandler(
        IHealthReportService healthReportService,
        ITenantContext tenantContext,
        ILogger<GenerateHealthReportCommandHandler> logger)
    {
        _healthReportService = healthReportService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<HealthReportDetailDto>> Handle(
        GenerateHealthReportCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Generating health report for tenant {TenantId}, type: {ReportType}",
                _tenantContext.TenantId,
                request.ReportType);

            var result = await _healthReportService.GenerateReportAsync(
                _tenantContext.TenantId,
                request.ReportType,
                cancellationToken);

            if (result.IsSuccess)
                _logger.LogInformation("Health report generated successfully");
            else
                _logger.LogWarning("Health report generation failed: {Error}", result.Error);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating health report");
            return Result<HealthReportDetailDto>.Fail($"Error generating health report: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for ScheduleHealthReportCommand.
/// </summary>
public class ScheduleHealthReportCommandHandler : IRequestHandler<ScheduleHealthReportCommand, Result<HealthReportDetailDto>>
{
    private readonly IHealthReportService _healthReportService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ScheduleHealthReportCommandHandler> _logger;

    public ScheduleHealthReportCommandHandler(
        IHealthReportService healthReportService,
        ITenantContext tenantContext,
        ILogger<ScheduleHealthReportCommandHandler> logger)
    {
        _healthReportService = healthReportService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<HealthReportDetailDto>> Handle(
        ScheduleHealthReportCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Scheduling health report {ReportId} for tenant {TenantId}, scheduled for {ScheduledFor}",
                request.ReportId,
                _tenantContext.TenantId,
                request.Request.ScheduledFor);

            var result = await _healthReportService.ScheduleReportAsync(
                _tenantContext.TenantId,
                request.ReportId,
                request.Request,
                cancellationToken);

            if (result.IsSuccess)
                _logger.LogInformation("Health report scheduled successfully: {ReportId}", request.ReportId);
            else
                _logger.LogWarning("Health report scheduling failed: {Error}", result.Error);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling health report {ReportId}", request.ReportId);
            return Result<HealthReportDetailDto>.Fail($"Error scheduling health report: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for SendHealthReportCommand.
/// </summary>
public class SendHealthReportCommandHandler : IRequestHandler<SendHealthReportCommand, Result<bool>>
{
    private readonly IHealthReportService _healthReportService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SendHealthReportCommandHandler> _logger;

    public SendHealthReportCommandHandler(
        IHealthReportService healthReportService,
        ITenantContext tenantContext,
        ILogger<SendHealthReportCommandHandler> logger)
    {
        _healthReportService = healthReportService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        SendHealthReportCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Sending health report {ReportId} to recipients: {Recipients}",
                request.ReportId,
                request.Recipients);

            var result = await _healthReportService.SendReportAsync(
                _tenantContext.TenantId,
                request.ReportId,
                request.Recipients,
                cancellationToken);

            if (result.IsSuccess)
                _logger.LogInformation("Health report sent successfully: {ReportId}", request.ReportId);
            else
                _logger.LogWarning("Health report send failed: {Error}", result.Error);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending health report {ReportId}", request.ReportId);
            return Result<bool>.Fail($"Error sending health report: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for ArchiveHealthReportCommand.
/// </summary>
public class ArchiveHealthReportCommandHandler : IRequestHandler<ArchiveHealthReportCommand, Result<bool>>
{
    private readonly IHealthReportService _healthReportService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ArchiveHealthReportCommandHandler> _logger;

    public ArchiveHealthReportCommandHandler(
        IHealthReportService healthReportService,
        ITenantContext tenantContext,
        ILogger<ArchiveHealthReportCommandHandler> logger)
    {
        _healthReportService = healthReportService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        ArchiveHealthReportCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Archiving health report {ReportId}", request.ReportId);

            var result = await _healthReportService.ArchiveReportAsync(
                _tenantContext.TenantId,
                request.ReportId,
                cancellationToken);

            if (result.IsSuccess)
                _logger.LogInformation("Health report archived successfully: {ReportId}", request.ReportId);
            else
                _logger.LogWarning("Health report archive failed: {Error}", result.Error);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving health report {ReportId}", request.ReportId);
            return Result<bool>.Fail($"Error archiving health report: {ex.Message}");
        }
    }
}
