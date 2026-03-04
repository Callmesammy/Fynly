namespace AiCFO.Domain.ValueObjects;

/// <summary>
/// Money value object - immutable, type-safe representation of money.
/// Always uses decimal for precision (never float or double).
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    /// <summary>
    /// Factory method to create Money with currency code.
    /// </summary>
    public static Money Create(decimal amount, CurrencyCode currencyCode)
    {
        var currency = Currency.FromCode(currencyCode);
        return new Money(amount, currency);
    }

    /// <summary>
    /// Add two Money amounts (must be same currency).
    /// </summary>
    public Money Add(Money other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        if (Currency.Code != other.Currency.Code)
            throw new InvalidOperationException(
                $"Cannot add {Currency.Code} and {other.Currency.Code}");

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Subtract two Money amounts (must be same currency).
    /// </summary>
    public Money Subtract(Money other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        if (Currency.Code != other.Currency.Code)
            throw new InvalidOperationException(
                $"Cannot subtract {other.Currency.Code} from {Currency.Code}");

        var result = Amount - other.Amount;
        if (result < 0)
            throw new InvalidOperationException("Result would be negative");

        return new Money(result, Currency);
    }

    /// <summary>
    /// Multiply Money by a percentage.
    /// </summary>
    public Money Multiply(decimal multiplier)
    {
        if (multiplier < 0)
            throw new ArgumentException("Multiplier cannot be negative", nameof(multiplier));

        return new Money(Amount * multiplier, Currency);
    }

    /// <summary>
    /// Multiply Money by a Percentage value object.
    /// </summary>
    public Money Multiply(Percentage percentage)
    {
        if (percentage is null)
            throw new ArgumentNullException(nameof(percentage));

        return Multiply(percentage.Decimal);
    }

    /// <summary>
    /// Divide Money by a divisor.
    /// </summary>
    public Money Divide(decimal divisor)
    {
        if (divisor <= 0)
            throw new ArgumentException("Divisor must be positive", nameof(divisor));

        return new Money(Amount / divisor, Currency);
    }

    /// <summary>
    /// Check if Money amount is zero.
    /// </summary>
    public bool IsZero => Amount == 0;

    /// <summary>
    /// Check if Money amount is positive.
    /// </summary>
    public bool IsPositive => Amount > 0;

    /// <summary>
    /// Check if Money amount is negative or zero.
    /// </summary>
    public bool IsNegativeOrZero => Amount <= 0;

    /// <summary>
    /// Compare two Money amounts (must be same currency).
    /// </summary>
    public int CompareTo(Money other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        if (Currency.Code != other.Currency.Code)
            throw new InvalidOperationException(
                $"Cannot compare {Currency.Code} and {other.Currency.Code}");

        return Amount.CompareTo(other.Amount);
    }

    /// <summary>
    /// Check if this Money is greater than other (must be same currency).
    /// </summary>
    public bool IsGreaterThan(Money other) => CompareTo(other) > 0;

    /// <summary>
    /// Check if this Money is less than other (must be same currency).
    /// </summary>
    public bool IsLessThan(Money other) => CompareTo(other) < 0;

    /// <summary>
    /// Format Money as string with currency symbol.
    /// </summary>
    public override string ToString()
    {
        var formattedAmount = Amount.ToString($"N{Currency.DecimalPlaces}");
        return $"{Currency.Symbol}{formattedAmount}";
    }

    /// <summary>
    /// Format Money as string with currency code (e.g., "1,500.00 NGN").
    /// </summary>
    public string ToStringWithCode()
    {
        var formattedAmount = Amount.ToString($"N{Currency.DecimalPlaces}");
        return $"{formattedAmount} {Currency.Code}";
    }

    /// <summary>
    /// Get Money amount as decimal (for database storage).
    /// </summary>
    public decimal ToDecimal() => Amount;

    /// <summary>
    /// Create Money from decimal (use when loading from database).
    /// </summary>
    public static Money FromDecimal(decimal amount, Currency currency)
    {
        return new Money(amount, currency);
    }
}
