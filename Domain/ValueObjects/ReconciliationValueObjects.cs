namespace AiCFO.Domain.ValueObjects;

/// <summary>
/// Reconciliation status enum - lifecycle of a reconciliation match.
/// </summary>
public enum ReconciliationStatus
{
    Proposed = 0,       // Auto-matched or manually suggested
    Confirmed = 1,      // User confirmed the match
    Rejected = 2,       // User rejected the match
    Manual = 3          // Manually matched by user
}

/// <summary>
/// Match type enum - indicates how the match was made.
/// </summary>
public enum MatchType
{
    ExactAmount = 0,           // Amount matches exactly
    AmountAndDate = 1,         // Amount and date match
    AmountAndDescription = 2,  // Amount and description match
    PartialAmount = 3,         // Amount partially matches (within threshold)
    Manual = 4                 // Manually matched by user
}

/// <summary>
/// Match confidence level - indicates how confident the auto-matcher is.
/// </summary>
public enum MatchConfidence
{
    VeryLow = 0,   // 0-20%
    Low = 1,       // 20-40%
    Medium = 2,    // 40-60%
    High = 3,      // 60-80%
    VeryHigh = 4   // 80-100%
}

/// <summary>
/// ReconciliationId value object - strongly typed ID for reconciliation matches.
/// </summary>
public sealed record ReconciliationId
{
    public Guid Value { get; init; }

    public ReconciliationId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Reconciliation ID cannot be empty", nameof(value));
        Value = value;
    }

    public static ReconciliationId Create() => new(Guid.NewGuid());

    public static implicit operator Guid(ReconciliationId id) => id.Value;
    public static implicit operator ReconciliationId(Guid id) => new(id);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// MatchScore value object - represents the confidence and scoring of a match.
/// </summary>
public sealed record MatchScore
{
    public decimal ConfidencePercentage { get; init; }  // 0-100
    public MatchConfidence ConfidenceLevel { get; init; }
    public MatchType MatchType { get; init; }
    public string MatchReason { get; init; }

    public MatchScore(
        decimal confidencePercentage,
        MatchType matchType,
        string matchReason)
    {
        if (confidencePercentage < 0 || confidencePercentage > 100)
            throw new ArgumentException("Confidence percentage must be between 0 and 100", nameof(confidencePercentage));

        if (string.IsNullOrWhiteSpace(matchReason))
            throw new ArgumentException("Match reason cannot be empty", nameof(matchReason));

        ConfidencePercentage = confidencePercentage;
        ConfidenceLevel = DetermineConfidenceLevel(confidencePercentage);
        MatchType = matchType;
        MatchReason = matchReason;
    }

    private static MatchConfidence DetermineConfidenceLevel(decimal percentage)
    {
        return percentage switch
        {
            >= 80 => MatchConfidence.VeryHigh,
            >= 60 => MatchConfidence.High,
            >= 40 => MatchConfidence.Medium,
            >= 20 => MatchConfidence.Low,
            _ => MatchConfidence.VeryLow
        };
    }

    public static MatchScore CreateExactMatch(string reason = "Exact match: amount and date")
        => new(100, MatchType.ExactAmount, reason);

    public static MatchScore CreatePartialMatch(decimal confidence, string reason)
        => new(confidence, MatchType.PartialAmount, reason);

    public static MatchScore CreateManualMatch()
        => new(100, MatchType.Manual, "Manually matched by user");
}

/// <summary>
/// VarianceAmount value object - represents the difference between matched amounts.
/// </summary>
public sealed record VarianceAmount
{
    public Money Amount { get; init; }
    public decimal Percentage { get; init; }  // Variance as percentage of transaction amount
    public bool IsSignificant { get; init; }  // > 1% variance

    public VarianceAmount(Money transactionAmount, Money journalAmount)
    {
        if (transactionAmount.Amount < 0 || journalAmount.Amount < 0)
            throw new ArgumentException("Amounts must be non-negative");

        Amount = transactionAmount.Subtract(journalAmount);
        
        if (transactionAmount.Amount > 0)
            Percentage = Math.Abs((Amount.Amount / transactionAmount.Amount) * 100);
        else
            Percentage = 0;

        IsSignificant = Percentage > 1;
    }

    public bool IsZeroVariance => Amount.Amount == 0;

    public static VarianceAmount Zero(CurrencyCode currencyCode)
        => new(Money.Create(0, currencyCode), Money.Create(0, currencyCode));
}

/// <summary>
/// TimelineVariance value object - represents the difference in dates between matched items.
/// </summary>
public sealed record TimelineVariance
{
    public TimeSpan TimeDifference { get; init; }
    public int DaysDifference { get; init; }
    public bool IsSignificant { get; init; }  // > 3 days

    public TimelineVariance(DateTime transactionDate, DateTime journalDate)
    {
        TimeDifference = transactionDate - journalDate;
        DaysDifference = Math.Abs(TimeDifference.Days);
        IsSignificant = DaysDifference > 3;
    }

    public bool IsExactDateMatch => TimeDifference.TotalDays == 0;

    public static TimelineVariance Zero => new(DateTime.UtcNow, DateTime.UtcNow);
}
