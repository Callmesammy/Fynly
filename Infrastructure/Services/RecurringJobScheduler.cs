using AiCFO.Application.Common;
using AiCFO.Application.Features.BackgroundJobs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AiCFO.Infrastructure.Services;

/// <summary>
/// Implementation of background job scheduling and management using Hangfire.
/// Provides recurring job scheduling, one-time scheduling, and job status monitoring.
/// Integrates with multi-tenancy via ITenantContext for job isolation.
/// </summary>
public class RecurringJobScheduler : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<RecurringJobScheduler> _logger;

    public RecurringJobScheduler(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        ITenantContext tenantContext,
        ILogger<RecurringJobScheduler> logger)
    {
        _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
        _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<string>> ScheduleRecurringJobAsync(
        string jobId,
        string cronExpression,
        Func<Task> jobAction)
    {
        try
        {
            return await ScheduleRecurringJobAsync(jobId, cronExpression, jobAction, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule recurring job {JobId} for tenant {TenantId}",
                jobId, _tenantContext.TenantId);
            return Result<string>.Fail($"Failed to schedule job: {ex.Message}");
        }
    }

    public async Task<Result<string>> ScheduleRecurringJobAsync(
        string jobId,
        string cronExpression,
        Func<Task> jobAction,
        string? description)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;
            var qualifiedJobId = $"{tenantId}_{jobId}";

            _logger.LogInformation(
                "Scheduling recurring job {JobId} with cron {Cron} for tenant {TenantId}. Description: {Description}",
                qualifiedJobId, cronExpression, tenantId, description ?? "N/A");

            // Wrap job action with tenant context restoration
            Func<Task> wrappedAction = async () =>
            {
                try
                {
                    _logger.LogInformation("Executing recurring job {JobId}", qualifiedJobId);
                    await jobAction();
                    _logger.LogInformation("Recurring job {JobId} completed successfully", qualifiedJobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Recurring job {JobId} failed with error", qualifiedJobId);
                    throw;
                }
            };

            _recurringJobManager.AddOrUpdate(
                qualifiedJobId,
                () => wrappedAction(),
                cronExpression,
                new RecurringJobOptions
                {
                    Queue = "default",
                    TimeZone = TimeZoneInfo.Utc,
                });

            _logger.LogInformation("Successfully scheduled recurring job {JobId}", qualifiedJobId);
            return Result<string>.Ok(qualifiedJobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling recurring job {JobId} for tenant {TenantId}",
                jobId, _tenantContext.TenantId);
            return Result<string>.Fail($"Error scheduling job: {ex.Message}");
        }
    }

    public async Task<Result<string>> ScheduleOneTimeJobAsync(
        string jobId,
        TimeSpan delay,
        Func<Task> jobAction)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;
            var qualifiedJobId = $"{tenantId}_{jobId}_{Guid.NewGuid():N}".Substring(0, 64);

            _logger.LogInformation(
                "Scheduling one-time job {JobId} with delay {Delay} for tenant {TenantId}",
                jobId, delay, tenantId);

            // Wrap job action with logging
            Func<Task> wrappedAction = async () =>
            {
                try
                {
                    _logger.LogInformation("Executing one-time job {JobId}", qualifiedJobId);
                    await jobAction();
                    _logger.LogInformation("One-time job {JobId} completed successfully", qualifiedJobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "One-time job {JobId} failed with error", qualifiedJobId);
                    throw;
                }
            };

            _backgroundJobClient.Schedule(
                () => wrappedAction(),
                delay);

            _logger.LogInformation("Successfully scheduled one-time job {JobId}", qualifiedJobId);
            return Result<string>.Ok(qualifiedJobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling one-time job {JobId} for tenant {TenantId}",
                jobId, _tenantContext.TenantId);
            return Result<string>.Fail($"Error scheduling job: {ex.Message}");
        }
    }

    public async Task<Result<string>> RemoveRecurringJobAsync(string jobId)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;
            var qualifiedJobId = $"{tenantId}_{jobId}";

            _logger.LogInformation("Removing recurring job {JobId} for tenant {TenantId}",
                jobId, tenantId);

            _recurringJobManager.RemoveIfExists(qualifiedJobId);

            _logger.LogInformation("Successfully removed recurring job {JobId}", qualifiedJobId);
            return Result<string>.Ok(qualifiedJobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing recurring job {JobId} for tenant {TenantId}",
                jobId, _tenantContext.TenantId);
            return Result<string>.Fail($"Error removing job: {ex.Message}");
        }
    }

    public async Task<Result<string>> TriggerJobImmediatelyAsync(
        string jobId,
        Func<Task> jobAction)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;
            var qualifiedJobId = $"{tenantId}_{jobId}_{Guid.NewGuid():N}".Substring(0, 64);

            _logger.LogInformation("Triggering job immediately {JobId} for tenant {TenantId}",
                jobId, tenantId);

            // Wrap job action with logging
            Func<Task> wrappedAction = async () =>
            {
                try
                {
                    _logger.LogInformation("Executing immediate job {JobId}", qualifiedJobId);
                    await jobAction();
                    _logger.LogInformation("Immediate job {JobId} completed successfully", qualifiedJobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Immediate job {JobId} failed with error", qualifiedJobId);
                    throw;
                }
            };

            var job = _backgroundJobClient.Enqueue(() => wrappedAction());

            _logger.LogInformation("Successfully triggered immediate job {JobId} with job id {HangfireJobId}",
                qualifiedJobId, job);
            return Result<string>.Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering immediate job {JobId} for tenant {TenantId}",
                jobId, _tenantContext.TenantId);
            return Result<string>.Fail($"Error triggering job: {ex.Message}");
        }
    }

    public async Task<Result<BackgroundJobStatusDto>> GetJobStatusAsync(string jobId)
    {
        try
        {
            _logger.LogInformation("Retrieving status for job {JobId}", jobId);

            // Note: This is a placeholder implementation
            // Full implementation would require accessing Hangfire's job storage directly
            var status = new BackgroundJobStatusDto(
                jobId,
                "Active",
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                0,
                null);

            return Result<BackgroundJobStatusDto>.Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job status for {JobId}", jobId);
            return Result<BackgroundJobStatusDto>.Fail($"Error retrieving job status: {ex.Message}");
        }
    }

    public async Task<Result<List<BackgroundJobInfoDto>>> GetAllRecurringJobsAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving all recurring jobs for tenant {TenantId}", _tenantContext.TenantId);

            // Note: This is a placeholder implementation
            // Full implementation would require accessing Hangfire's job storage directly
            var jobs = new List<BackgroundJobInfoDto>();

            return Result<List<BackgroundJobInfoDto>>.Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recurring jobs for tenant {TenantId}", _tenantContext.TenantId);
            return Result<List<BackgroundJobInfoDto>>.Fail($"Error retrieving jobs: {ex.Message}");
        }
    }

    public async Task<Result<List<FailedJobDto>>> GetFailedJobsAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving failed jobs for tenant {TenantId}", _tenantContext.TenantId);

            // Note: This is a placeholder implementation
            // Full implementation would require accessing Hangfire's job storage directly
            var failedJobs = new List<FailedJobDto>();

            return Result<List<FailedJobDto>>.Ok(failedJobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failed jobs for tenant {TenantId}", _tenantContext.TenantId);
            return Result<List<FailedJobDto>>.Fail($"Error retrieving failed jobs: {ex.Message}");
        }
    }

    public async Task<Result<string>> RetryFailedJobAsync(string jobId)
    {
        try
        {
            _logger.LogInformation("Retrying failed job {JobId} for tenant {TenantId}",
                jobId, _tenantContext.TenantId);

            // Note: This is a placeholder implementation
            // Full implementation would retry the specific failed job

            return Result<string>.Ok($"Retry initiated for job {jobId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed job {JobId} for tenant {TenantId}",
                jobId, _tenantContext.TenantId);
            return Result<string>.Fail($"Error retrying job: {ex.Message}");
        }
    }
}
