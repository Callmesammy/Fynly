using AiCFO.Domain.ValueObjects;

namespace AiCFO.Application.Common;

/// <summary>
/// Service abstraction for detecting financial anomalies.
/// Identifies unusual transactions, patterns, and risk indicators.
/// </summary>
public interface IAnomalyDetectionService
{
    /// <summary>
    /// Analyze a single journal entry for anomalies.
    /// Checks amount, frequency, category, and behavioral patterns.
    /// </summary>
    Task<Result<AnomalyDetectionResult>> AnalyzeJournalEntryAsync(
        Guid journalEntryId,
        Guid accountId,
        Money amount,
        AccountType accountType,
        DateTime transactionDate);

    /// <summary>
    /// Analyze a bank transaction for anomalies.
    /// Compares against historical patterns and thresholds.
    /// </summary>
    Task<Result<AnomalyDetectionResult>> AnalyzeBankTransactionAsync(
        Guid bankTransactionId,
        Money amount,
        string? description,
        DateTime transactionDate);

    /// <summary>
    /// Detect unusual amount patterns for an account.
    /// Identifies outliers and statistical deviations.
    /// </summary>
    Task<Result<List<AnomalyScore>>> DetectUnusualAmountsAsync(
        Guid accountId,
        int lookbackDays = 90);

    /// <summary>
    /// Detect unusual transaction frequency for an account.
    /// Identifies sudden increases/decreases in activity.
    /// </summary>
    Task<Result<List<AnomalyScore>>> DetectUnusualFrequencyAsync(
        Guid accountId,
        int lookbackDays = 90);

    /// <summary>
    /// Detect duplicate or suspicious patterns.
    /// Identifies identical amounts, descriptions, or sequences.
    /// </summary>
    Task<Result<List<AnomalyScore>>> DetectDuplicatePatternsAsync(
        Guid accountId,
        int lookbackDays = 90);

    /// <summary>
    /// Detect time-series deviations (seasonal/trending anomalies).
    /// Uses moving averages and trend analysis.
    /// </summary>
    Task<Result<List<AnomalyScore>>> DetectTimeSeriesAnomaliesAsync(
        Guid accountId,
        int lookbackDays = 90);

    /// <summary>
    /// Scan all unmatched transactions for anomalies.
    /// Prioritizes by severity and confidence.
    /// </summary>
    Task<Result<List<AnomalyDto>>> ScanUnmatchedTransactionsAsync(
        AnomalySeverity minSeverity = AnomalySeverity.Medium);

    /// <summary>
    /// Get all recent anomalies across tenant.
    /// Returns sorted by severity and recency.
    /// </summary>
    Task<Result<List<AnomalyDto>>> GetRecentAnomaliesAsync(
        int days = 30,
        AnomalySeverity? severityFilter = null);

    /// <summary>
    /// Dismiss an anomaly alert (mark as reviewed).
    /// </summary>
    Task<Result<bool>> DismissAnomalyAsync(
        Guid anomalyId,
        string? notes = null);

    /// <summary>
    /// Flag a transaction as false positive.
    /// Helps improve anomaly detection accuracy.
    /// </summary>
    Task<Result<bool>> FlagFalsePositiveAsync(
        Guid anomalyId,
        string reason);

    /// <summary>
    /// Get anomaly statistics and trends.
    /// Returns summary metrics and health indicators.
    /// </summary>
    Task<Result<AnomalyStatsDto>> GetAnomalyStatsAsync();
}

/// <summary>
/// Result from single anomaly analysis.
/// </summary>
public record AnomalyDetectionResult(
    bool HasAnomalies,
    List<AnomalyScore> Anomalies,
    decimal OverallRiskScore,
    string Summary);

/// <summary>
/// Data transfer object for anomaly alerts.
/// </summary>
public record AnomalyDto(
    Guid Id,
    AnomalyScore Score,
    Guid RelatedEntityId,
    string EntityType, // "JournalEntry" | "BankTransaction"
    Money? Amount,
    DateTime DetectedAt,
    DateTime? DismissedAt,
    string? DismissalNotes);

/// <summary>
/// Anomaly statistics and trends.
/// </summary>
public record AnomalyStatsDto(
    int TotalAnomalies,
    int CriticalCount,
    int HighCount,
    int MediumCount,
    int LowCount,
    decimal AverageConfidence,
    List<AnomalyTrendDto> Trends,
    DateTime CalculatedAt);

/// <summary>
/// Anomaly trend over time period.
/// </summary>
public record AnomalyTrendDto(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int Count,
    decimal AverageConfidence,
    AnomalySeverity MostCommonSeverity);
