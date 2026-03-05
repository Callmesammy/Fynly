namespace AiCFO.Domain.Entities;

using AiCFO.Domain.Abstractions;

/// <summary>
/// Report status enumeration for health report lifecycle.
/// </summary>
public enum ReportStatus
{
    Generated = 0,  // Report freshly generated
    Scheduled = 1,  // Report scheduled for later
    Archived = 2,   // Report archived/old
    Sent = 3        // Report sent to user
}

/// <summary>
/// Report frequency enumeration for scheduled reports.
/// </summary>
public enum ReportFrequency
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    OnDemand = 4
}

/// <summary>
/// Report type enumeration for different health report variants.
/// </summary>
public enum ReportType
{
    Overall = 0,        // Complete health assessment
    ByDimension = 1,    // Detailed dimension breakdown
    Trend = 2,          // Historical trends
    Executive = 3       // Executive summary
}

/// <summary>
/// Represents a scheduled or generated health assessment report.
/// Tracks health scores, dimensions, recommendations, and distribution.
/// </summary>
public class HealthReport : AggregateRoot
{
    /// <summary>
    /// Unique report identifier.
    /// </summary>
    public Guid ReportId { get; private set; }

    /// <summary>
    /// Report type (Overall, ByDimension, Trend, Executive).
    /// </summary>
    public ReportType ReportType { get; private set; }

    /// <summary>
    /// Report status (Generated, Scheduled, Archived, Sent).
    /// </summary>
    public ReportStatus Status { get; private set; }

    /// <summary>
    /// Overall health score (0-100).
    /// </summary>
    public decimal OverallScore { get; private set; }

    /// <summary>
    /// Health rating (e.g., "Excellent", "Good", "Fair", "Poor").
    /// </summary>
    public string Rating { get; private set; }

    /// <summary>
    /// Liquidity score (0-100).
    /// </summary>
    public decimal LiquidityScore { get; private set; }

    /// <summary>
    /// Profitability score (0-100).
    /// </summary>
    public decimal ProfitabilityScore { get; private set; }

    /// <summary>
    /// Solvency score (0-100).
    /// </summary>
    public decimal SolvencyScore { get; private set; }

    /// <summary>
    /// Efficiency score (0-100).
    /// </summary>
    public decimal EfficiencyScore { get; private set; }

    /// <summary>
    /// Growth score (0-100).
    /// </summary>
    public decimal GrowthScore { get; private set; }

    /// <summary>
    /// Number of critical recommendations included.
    /// </summary>
    public int CriticalRecommendationsCount { get; private set; }

    /// <summary>
    /// Number of high-priority recommendations included.
    /// </summary>
    public int HighRecommendationsCount { get; private set; }

    /// <summary>
    /// Total anomalies detected during assessment.
    /// </summary>
    public int AnomaliesDetected { get; private set; }

    /// <summary>
    /// Timestamp when report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; private set; }

    /// <summary>
    /// Timestamp when report is/was scheduled to be sent.
    /// </summary>
    public DateTime? ScheduledFor { get; private set; }

    /// <summary>
    /// Timestamp when report was actually sent.
    /// </summary>
    public DateTime? SentAt { get; private set; }

    /// <summary>
    /// Timestamp when report expires (for archival).
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// Frequency for recurring reports (Daily, Weekly, Monthly, etc).
    /// </summary>
    public ReportFrequency? Frequency { get; private set; }

    /// <summary>
    /// Email recipients for the report (comma-separated or JSON array).
    /// </summary>
    public string? Recipients { get; private set; }

    /// <summary>
    /// Summary/highlights of the report.
    /// </summary>
    public string? Summary { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
#pragma warning disable CS8618
    private HealthReport() { }
#pragma warning restore CS8618

    /// <summary>
    /// Constructor for creating health report.
    /// </summary>
    private HealthReport(
        Guid id,
        Guid tenantId,
        Guid createdBy,
        ReportType reportType,
        decimal overallScore,
        string rating,
        decimal liquidityScore,
        decimal profitabilityScore,
        decimal solvencyScore,
        decimal efficiencyScore,
        decimal growthScore,
        int criticalRecommendations,
        int highRecommendations,
        int anomalies)
        : base(id, tenantId, createdBy)
    {
        ReportId = id;
        ReportType = reportType;
        Status = ReportStatus.Generated;
        OverallScore = overallScore;
        Rating = rating;
        LiquidityScore = liquidityScore;
        ProfitabilityScore = profitabilityScore;
        SolvencyScore = solvencyScore;
        EfficiencyScore = efficiencyScore;
        GrowthScore = growthScore;
        CriticalRecommendationsCount = criticalRecommendations;
        HighRecommendationsCount = highRecommendations;
        AnomaliesDetected = anomalies;
        GeneratedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method to create a new health report.
    /// </summary>
    public static HealthReport Create(
        Guid tenantId,
        Guid createdBy,
        ReportType reportType,
        decimal overallScore,
        string rating,
        decimal liquidityScore,
        decimal profitabilityScore,
        decimal solvencyScore,
        decimal efficiencyScore,
        decimal growthScore,
        int criticalRecommendations = 0,
        int highRecommendations = 0,
        int anomalies = 0)
    {
        return new HealthReport(
            Guid.NewGuid(),
            tenantId,
            createdBy,
            reportType,
            overallScore,
            rating,
            liquidityScore,
            profitabilityScore,
            solvencyScore,
            efficiencyScore,
            growthScore,
            criticalRecommendations,
            highRecommendations,
            anomalies);
    }

    /// <summary>
    /// Schedule the report for future delivery.
    /// </summary>
    public void Schedule(DateTime scheduledFor, ReportFrequency frequency)
    {
        ScheduledFor = scheduledFor;
        Frequency = frequency;
        Status = ReportStatus.Scheduled;
    }

    /// <summary>
    /// Mark report as sent.
    /// </summary>
    public void MarkAsSent(string? recipients = null)
    {
        Status = ReportStatus.Sent;
        SentAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(recipients))
            Recipients = recipients;
    }

    /// <summary>
    /// Archive the report.
    /// </summary>
    public void Archive(int expirationDays = 90)
    {
        Status = ReportStatus.Archived;
        ExpiresAt = DateTime.UtcNow.AddDays(expirationDays);
    }

    /// <summary>
    /// Set summary/highlights for the report.
    /// </summary>
    public void SetSummary(string summary)
    {
        Summary = summary;
    }
}
