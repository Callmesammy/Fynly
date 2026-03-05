namespace AiCFO.Application.Features.HealthReports.Queries;

using AiCFO.Application.Common;
using AiCFO.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Query to retrieve a specific health report.
/// </summary>
public record GetHealthReportQuery(Guid ReportId) : IRequest<Result<HealthReportDetailDto>>;

/// <summary>
/// Query to list health reports with optional filtering.
/// </summary>
public record ListHealthReportsQuery(
    ReportStatus? StatusFilter = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) 
    : IRequest<Result<List<HealthReportSummaryDto>>>;

/// <summary>
/// Query to retrieve scheduled health reports.
/// </summary>
public record GetScheduledHealthReportsQuery() : IRequest<Result<List<ScheduledReportDto>>>;

/// <summary>
/// Query to retrieve health report statistics.
/// </summary>
public record GetHealthReportStatisticsQuery(DateTime? FromDate = null, DateTime? ToDate = null) 
    : IRequest<Result<HealthReportStatisticsDto>>;

/// <summary>
/// Handler for GetHealthReportQuery.
/// </summary>
public class GetHealthReportQueryHandler : IRequestHandler<GetHealthReportQuery, Result<HealthReportDetailDto>>
{
    private readonly IHealthReportService _healthReportService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetHealthReportQueryHandler> _logger;

    public GetHealthReportQueryHandler(
        IHealthReportService healthReportService,
        ITenantContext tenantContext,
        ILogger<GetHealthReportQueryHandler> logger)
    {
        _healthReportService = healthReportService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<HealthReportDetailDto>> Handle(
        GetHealthReportQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving health report {ReportId}", request.ReportId);

            var result = await _healthReportService.GetReportAsync(
                _tenantContext.TenantId,
                request.ReportId,
                cancellationToken);

            if (!result.IsSuccess)
                _logger.LogWarning("Health report not found: {ReportId}", request.ReportId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health report {ReportId}", request.ReportId);
            return Result<HealthReportDetailDto>.Fail($"Error retrieving health report: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for ListHealthReportsQuery.
/// </summary>
public class ListHealthReportsQueryHandler : IRequestHandler<ListHealthReportsQuery, Result<List<HealthReportSummaryDto>>>
{
    private readonly IHealthReportService _healthReportService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ListHealthReportsQueryHandler> _logger;

    public ListHealthReportsQueryHandler(
        IHealthReportService healthReportService,
        ITenantContext tenantContext,
        ILogger<ListHealthReportsQueryHandler> logger)
    {
        _healthReportService = healthReportService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<List<HealthReportSummaryDto>>> Handle(
        ListHealthReportsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Listing health reports for tenant {TenantId}, status filter: {StatusFilter}",
                _tenantContext.TenantId,
                request.StatusFilter?.ToString() ?? "None");

            var result = await _healthReportService.ListReportsAsync(
                _tenantContext.TenantId,
                request.StatusFilter,
                request.FromDate,
                request.ToDate,
                cancellationToken);

            if (result.IsSuccess)
                _logger.LogInformation("Retrieved {Count} health reports", result.Value?.Count ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing health reports");
            return Result<List<HealthReportSummaryDto>>.Fail($"Error listing health reports: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for GetScheduledHealthReportsQuery.
/// </summary>
public class GetScheduledHealthReportsQueryHandler : IRequestHandler<GetScheduledHealthReportsQuery, Result<List<ScheduledReportDto>>>
{
    private readonly IHealthReportService _healthReportService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetScheduledHealthReportsQueryHandler> _logger;

    public GetScheduledHealthReportsQueryHandler(
        IHealthReportService healthReportService,
        ITenantContext tenantContext,
        ILogger<GetScheduledHealthReportsQueryHandler> logger)
    {
        _healthReportService = healthReportService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<List<ScheduledReportDto>>> Handle(
        GetScheduledHealthReportsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving scheduled health reports for tenant {TenantId}", _tenantContext.TenantId);

            var result = await _healthReportService.GetScheduledReportsAsync(
                _tenantContext.TenantId,
                cancellationToken);

            if (result.IsSuccess)
                _logger.LogInformation("Retrieved {Count} scheduled reports", result.Value?.Count ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scheduled health reports");
            return Result<List<ScheduledReportDto>>.Fail($"Error retrieving scheduled reports: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for GetHealthReportStatisticsQuery.
/// </summary>
public class GetHealthReportStatisticsQueryHandler : IRequestHandler<GetHealthReportStatisticsQuery, Result<HealthReportStatisticsDto>>
{
    private readonly IHealthReportService _healthReportService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetHealthReportStatisticsQueryHandler> _logger;

    public GetHealthReportStatisticsQueryHandler(
        IHealthReportService healthReportService,
        ITenantContext tenantContext,
        ILogger<GetHealthReportStatisticsQueryHandler> logger)
    {
        _healthReportService = healthReportService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<HealthReportStatisticsDto>> Handle(
        GetHealthReportStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Calculating health report statistics for tenant {TenantId}, from {FromDate} to {ToDate}",
                _tenantContext.TenantId,
                request.FromDate?.ToString("yyyy-MM-dd") ?? "N/A",
                request.ToDate?.ToString("yyyy-MM-dd") ?? "N/A");

            var result = await _healthReportService.GetReportStatisticsAsync(
                _tenantContext.TenantId,
                request.FromDate,
                request.ToDate,
                cancellationToken);

            if (result.IsSuccess)
                _logger.LogInformation("Statistics calculated: {TotalReports} reports, avg score: {AvgScore}",
                    result.Value?.TotalReportsGenerated,
                    result.Value?.AverageOverallScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating health report statistics");
            return Result<HealthReportStatisticsDto>.Fail($"Error calculating statistics: {ex.Message}");
        }
    }
}
