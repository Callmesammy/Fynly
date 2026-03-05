using AiCFO.Domain.ValueObjects;

namespace AiCFO.Application.Common;

/// <summary>
/// Service abstraction for financial forecasting and predictions.
/// Generates predictions based on historical patterns and machine learning models.
/// </summary>
public interface IPredictionService
{
    /// <summary>
    /// Predict future account balance at specified date.
    /// Uses trend analysis and seasonal patterns.
    /// </summary>
    Task<Result<FinancialPrediction>> PredictAccountBalanceAsync(
        Guid accountId,
        DateTime predictionDate);

    /// <summary>
    /// Predict cash flow for specified period.
    /// Returns expected inflows and outflows.
    /// </summary>
    Task<Result<CashFlowPredictionDto>> PredictCashFlowAsync(
        DateTime periodStart,
        DateTime periodEnd);

    /// <summary>
    /// Predict revenue for specified period.
    /// Analyzes income accounts and historical trends.
    /// </summary>
    Task<Result<FinancialPrediction>> PredictRevenueAsync(
        DateTime periodStart,
        DateTime periodEnd);

    /// <summary>
    /// Predict expenses for specified period.
    /// Analyzes expense accounts and spending patterns.
    /// </summary>
    Task<Result<FinancialPrediction>> PredictExpensesAsync(
        DateTime periodStart,
        DateTime periodEnd);

    /// <summary>
    /// Predict profitability (Revenue - Expenses) for period.
    /// </summary>
    Task<Result<FinancialPrediction>> PredictProfitabilityAsync(
        DateTime periodStart,
        DateTime periodEnd);

    /// <summary>
    /// Predict when account will reach specified balance.
    /// Useful for cash runway and liquidity planning.
    /// </summary>
    Task<Result<DatePredictionDto>> PredictWhenBalanceReachedAsync(
        Guid accountId,
        Money targetBalance);

    /// <summary>
    /// Get all recent predictions for dashboard/reporting.
    /// Returns sorted by most recent and highest confidence.
    /// </summary>
    Task<Result<List<FinancialPredictionDto>>> GetRecentPredictionsAsync(
        int days = 30);

    /// <summary>
    /// Compare predicted vs actual results.
    /// Helps evaluate model accuracy over time.
    /// </summary>
    Task<Result<PredictionAccuracyDto>> EvaluatePredictionAccuracyAsync(
        DateTime evaluationStart,
        DateTime evaluationEnd);

    /// <summary>
    /// Get prediction confidence distribution.
    /// Returns confidence intervals and probability ranges.
    /// </summary>
    Task<Result<PredictionConfidenceDto>> GetConfidenceAnalysisAsync(
        Guid accountId);

    /// <summary>
    /// Identify predictable vs unpredictable accounts.
    /// Helps determine which predictions are most reliable.
    /// </summary>
    Task<Result<List<PredictabilityScoreDto>>> GetPredictabilityScoresAsync();

    /// <summary>
    /// Generate trend-based forecast for all key accounts.
    /// Used for comprehensive financial planning.
    /// </summary>
    Task<Result<ComprehensiveForecastDto>> GenerateTrendForecastAsync(
        int forecastMonths = 3);

    /// <summary>
    /// Retrain prediction models using latest data.
    /// Should be run periodically (weekly/monthly).
    /// </summary>
    Task<Result<bool>> RetrainModelsAsync();
}

/// <summary>
/// Cash flow prediction (inflows + outflows).
/// </summary>
public record CashFlowPredictionDto(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    FinancialPrediction PredictedInflows,
    FinancialPrediction PredictedOutflows,
    FinancialPrediction NetCashFlow,
    decimal ConfidencePercentage);

/// <summary>
/// When will account reach target balance.
/// </summary>
public record DatePredictionDto(
    Guid AccountId,
    Money TargetBalance,
    DateTime? PredictedDate,
    decimal ConfidencePercentage,
    string Interpretation); // "On track" | "Will exceed" | "Unreachable" | "Already exceeded"

/// <summary>
/// Prediction DTO for API serialization.
/// </summary>
public record FinancialPredictionDto(
    Guid Id,
    Money PredictedAmount,
    decimal ConfidencePercentage,
    PredictionAccuracy AccuracyLevel,
    DateTime PredictionPeriodStart,
    DateTime PredictionPeriodEnd,
    string Model,
    Dictionary<string, decimal> TopFactors,
    DateTime CreatedAt,
    DateTime? ValidatedAt,
    string Status); // "Active" | "Expired" | "Validated"

/// <summary>
/// Prediction accuracy metrics.
/// </summary>
public record PredictionAccuracyDto(
    decimal MeanAbsolutePercentageError, // MAPE
    decimal RootMeanSquaredError,         // RMSE
    int SampleSize,
    DateTime EvaluationStart,
    DateTime EvaluationEnd,
    string OverallAccuracyLevel);

/// <summary>
/// Confidence distribution analysis.
/// </summary>
public record PredictionConfidenceDto(
    decimal MinConfidence,
    decimal MaxConfidence,
    decimal AverageConfidence,
    decimal MedianConfidence,
    int VeryHighCount,     // >= 90%
    int HighCount,         // 70-89%
    int ModerateCount,     // 50-69%
    int LowCount);         // < 50%

/// <summary>
/// How predictable is an account.
/// </summary>
public record PredictabilityScoreDto(
    Guid AccountId,
    string AccountName,
    decimal PredictabilityScore, // 0-100
    string Category,             // "Highly Predictable" | "Predictable" | "Somewhat Unpredictable" | "Unpredictable"
    string Reason,
    int DataPointsUsed);

/// <summary>
/// Comprehensive multi-month forecast.
/// </summary>
public record ComprehensiveForecastDto(
    DateTime GeneratedAt,
    int ForecastMonths,
    List<MonthlyForecastDto> MonthlyForecasts,
    FinancialPrediction TotalRevenuePrediction,
    FinancialPrediction TotalExpensePrediction,
    FinancialPrediction TotalProfitPrediction,
    decimal OverallConfidence);

/// <summary>
/// Monthly forecast details.
/// </summary>
public record MonthlyForecastDto(
    int MonthNumber,
    DateTime MonthStart,
    DateTime MonthEnd,
    FinancialPrediction RevenueForcast,
    FinancialPrediction ExpenseForecast,
    FinancialPrediction NetProfitForecast,
    decimal SeasonalAdjustment);
