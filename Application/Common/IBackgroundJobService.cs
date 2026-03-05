using AiCFO.Application.Common;

namespace AiCFO.Application.Features.BackgroundJobs;

/// <summary>
/// Abstraction for background job scheduling and management.
/// Enables recurring jobs (daily, hourly, etc.) and one-time scheduled jobs.
/// Supports multi-tenancy through ITenantContext.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Schedule a recurring job that runs at a fixed interval or cron expression.
    /// </summary>
    /// <param name="jobId">Unique identifier for the job (e.g., "anomaly-detection-daily")</param>
    /// <param name="cronExpression">Cron expression (e.g., "0 2 * * *" for 2 AM daily)</param>
    /// <param name="jobAction">Action to execute when job runs</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<string>> ScheduleRecurringJobAsync(
        string jobId, 
        string cronExpression, 
        Func<Task> jobAction);

    /// <summary>
    /// Schedule a recurring job with a description for the Hangfire dashboard.
    /// </summary>
    Task<Result<string>> ScheduleRecurringJobAsync(
        string jobId,
        string cronExpression,
        Func<Task> jobAction,
        string description);

    /// <summary>
    /// Schedule a one-time job to run after a delay.
    /// </summary>
    /// <param name="jobId">Unique identifier for the job</param>
    /// <param name="delay">How long to wait before executing</param>
    /// <param name="jobAction">Action to execute</param>
    Task<Result<string>> ScheduleOneTimeJobAsync(
        string jobId,
        TimeSpan delay,
        Func<Task> jobAction);

    /// <summary>
    /// Remove (unschedule) a recurring job.
    /// </summary>
    Task<Result<string>> RemoveRecurringJobAsync(string jobId);

    /// <summary>
    /// Trigger a job immediately (fire-and-forget execution).
    /// </summary>
    Task<Result<string>> TriggerJobImmediatelyAsync(
        string jobId,
        Func<Task> jobAction);

    /// <summary>
    /// Get the status of a scheduled job.
    /// </summary>
    Task<Result<BackgroundJobStatusDto>> GetJobStatusAsync(string jobId);

    /// <summary>
    /// Get all scheduled recurring jobs.
    /// </summary>
    Task<Result<List<BackgroundJobInfoDto>>> GetAllRecurringJobsAsync();

    /// <summary>
    /// Get all failed jobs.
    /// </summary>
    Task<Result<List<FailedJobDto>>> GetFailedJobsAsync();

    /// <summary>
    /// Retry a failed job.
    /// </summary>
    Task<Result<string>> RetryFailedJobAsync(string jobId);
}

/// <summary>
/// Data transfer object for background job status.
/// </summary>
public record BackgroundJobStatusDto(
    string JobId,
    string Status,
    DateTime? CreatedAt,
    DateTime? NextExecution,
    int AttemptCount,
    string? LastError);

/// <summary>
/// Data transfer object for background job information.
/// </summary>
public record BackgroundJobInfoDto(
    string JobId,
    string Cron,
    string Status,
    DateTime? LastExecution,
    DateTime? NextExecution,
    string? Description);

/// <summary>
/// Data transfer object for failed job information.
/// </summary>
public record FailedJobDto(
    string JobId,
    DateTime FailedAt,
    string ErrorMessage,
    string? StackTrace);
