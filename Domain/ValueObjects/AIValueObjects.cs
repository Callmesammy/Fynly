namespace AiCFO.Domain.ValueObjects;

/// <summary>
/// Anomaly severity levels
/// </summary>
public enum AnomalySeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Anomaly types for financial transactions
/// </summary>
public enum AnomalyType
{
    UnusualAmount = 0,
    UnusualFrequency = 1,
    UnusualCategory = 2,
    OutlierAmount = 3,
    DuplicatePattern = 4,
    TimeSeriesDeviation = 5,
    BehavioralChange = 6,
    RiskIndicator = 7
}

/// <summary>
/// Represents a financial anomaly score and metadata
/// </summary>
public record AnomalyScore(
    decimal ConfidencePercentage,    // 0-100% confidence this is an anomaly
    AnomalySeverity Severity,        // Low, Medium, High, Critical
    AnomalyType AnomalyType,         // Type of anomaly detected
    string Reason,                   // Human-readable explanation
    Dictionary<string, object> Metrics) // Additional diagnostic data
{
    /// <summary>
    /// Validate anomaly score
    /// </summary>
    public static AnomalyScore Create(
        decimal confidence,
        AnomalySeverity severity,
        AnomalyType type,
        string reason,
        Dictionary<string, object>? metrics = null)
    {
        if (confidence < 0 || confidence > 100)
            throw new ArgumentException("Confidence must be between 0 and 100", nameof(confidence));
        
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));

        return new AnomalyScore(
            confidence,
            severity,
            type,
            reason,
            metrics ?? new());
    }

    /// <summary>
    /// Is this a significant anomaly?
    /// </summary>
    public bool IsSignificant => Severity >= AnomalySeverity.Medium && ConfidencePercentage >= 70;
}

/// <summary>
/// Prediction confidence level
/// </summary>
public enum PredictionAccuracy
{
    VeryLow = 0,      // < 60%
    Low = 1,          // 60-70%
    Moderate = 2,     // 70-80%
    High = 3,         // 80-90%
    VeryHigh = 4      // > 90%
}

/// <summary>
/// Represents a financial prediction
/// </summary>
public record FinancialPrediction(
    Money PredictedAmount,
    decimal ConfidencePercentage,
    PredictionAccuracy AccuracyLevel,
    DateTime PredictionDate,
    DateTime PredictionPeriodStart,
    DateTime PredictionPeriodEnd,
    string Model,                          // ML model used
    Dictionary<string, decimal> TopFactors) // Top contributing factors
{
    /// <summary>
    /// Create a prediction
    /// </summary>
    public static FinancialPrediction Create(
        Money amount,
        decimal confidence,
        DateTime predictionDate,
        DateTime periodStart,
        DateTime periodEnd,
        string model,
        Dictionary<string, decimal>? factors = null)
    {
        if (confidence < 0 || confidence > 100)
            throw new ArgumentException("Confidence must be between 0 and 100", nameof(confidence));

        var accuracy = confidence switch
        {
            >= 90 => PredictionAccuracy.VeryHigh,
            >= 80 => PredictionAccuracy.High,
            >= 70 => PredictionAccuracy.Moderate,
            >= 60 => PredictionAccuracy.Low,
            _ => PredictionAccuracy.VeryLow
        };

        return new FinancialPrediction(
            amount,
            confidence,
            accuracy,
            predictionDate,
            periodStart,
            periodEnd,
            model,
            factors ?? new());
    }

    /// <summary>
    /// Get prediction range (confidence interval)
    /// </summary>
    public (Money Lower, Money Upper) GetConfidenceInterval(decimal marginPercent = 5m)
    {
        var margin = PredictedAmount.Amount * (marginPercent / 100);
        var lower = new Money(PredictedAmount.Amount - margin, PredictedAmount.Currency);
        var upper = new Money(PredictedAmount.Amount + margin, PredictedAmount.Currency);
        return (lower, upper);
    }
}

/// <summary>
/// Financial health score (0-100)
/// </summary>
public record FinancialHealthScore(
    decimal Score,                   // 0-100
    string Rating,                   // Excellent, Good, Fair, Poor
    List<string> Strengths,
    List<string> Weaknesses,
    List<string> Recommendations,
    DateTime CalculatedAt)
{
    /// <summary>
    /// Create health score
    /// </summary>
    public static FinancialHealthScore Create(
        decimal score,
        List<string> strengths,
        List<string> weaknesses,
        List<string> recommendations)
    {
        if (score < 0 || score > 100)
            throw new ArgumentException("Score must be between 0 and 100", nameof(score));

        var rating = score switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Fair",
            _ => "Poor"
        };

        return new FinancialHealthScore(
            score,
            rating,
            strengths ?? new(),
            weaknesses ?? new(),
            recommendations ?? new(),
            DateTime.UtcNow);
    }

    /// <summary>
    /// Is health score healthy?
    /// </summary>
    public bool IsHealthy => Score >= 60;
}

/// <summary>
/// AI recommendation priority
/// </summary>
public enum RecommendationPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Represents an AI-generated recommendation
/// </summary>
public record AIRecommendation(
    string Title,
    string Description,
    RecommendationPriority Priority,
    decimal ImpactEstimate,          // Estimated % impact (positive or negative)
    string Category,                 // e.g., "Cost Reduction", "Revenue Growth", "Risk Mitigation"
    string ActionItems,              // Steps to implement
    Dictionary<string, object> SupportingData)
{
    /// <summary>
    /// Create recommendation
    /// </summary>
    public static AIRecommendation Create(
        string title,
        string description,
        RecommendationPriority priority,
        decimal impact,
        string category,
        string actions,
        Dictionary<string, object>? data = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (impact < -100 || impact > 100)
            throw new ArgumentException("Impact must be between -100 and 100", nameof(impact));

        return new AIRecommendation(
            title,
            description,
            priority,
            impact,
            category,
            actions,
            data ?? new());
    }

    /// <summary>
    /// Is this a high-priority recommendation?
    /// </summary>
    public bool IsUrgent => Priority >= RecommendationPriority.High && Math.Abs(ImpactEstimate) >= 5;
}

/// <summary>
/// Time series trend direction
/// </summary>
public enum TrendDirection
{
    StronglyDecreasing = 0,
    Decreasing = 1,
    Stable = 2,
    Increasing = 3,
    StronglyIncreasing = 4
}

/// <summary>
/// Represents a financial trend
/// </summary>
public record FinancialTrend(
    TrendDirection Direction,
    decimal TrendStrength,           // 0-100, how strong is the trend
    decimal GrowthRate,              // % change over period
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int DataPointCount)
{
    /// <summary>
    /// Create trend
    /// </summary>
    public static FinancialTrend Create(
        TrendDirection direction,
        decimal strength,
        decimal rate,
        DateTime start,
        DateTime end,
        int pointCount)
    {
        if (strength < 0 || strength > 100)
            throw new ArgumentException("Strength must be between 0 and 100", nameof(strength));

        if (pointCount < 2)
            throw new ArgumentException("Need at least 2 data points", nameof(pointCount));

        return new FinancialTrend(direction, strength, rate, start, end, pointCount);
    }

    /// <summary>
    /// Is trend accelerating?
    /// </summary>
    public bool IsAccelerating => Direction == TrendDirection.StronglyIncreasing || 
                                   Direction == TrendDirection.StronglyDecreasing;
}
