namespace AiCFO.Domain.ValueObjects;

/// <summary>
/// Percentage value object - immutable representation of a percentage.
/// Always stored as decimal (0.0 to 100.0 represents 0% to 100%).
/// </summary>
public sealed record Percentage
{
    public decimal Value { get; }

    public Percentage(decimal value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentException("Percentage must be between 0 and 100", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Get percentage as decimal (e.g., 25% = 0.25).
    /// </summary>
    public decimal Decimal => Value / 100;

    /// <summary>
    /// Get percentage value (e.g., 25% = 25).
    /// </summary>
    public decimal ToDecimal() => Value;

    /// <summary>
    /// Factory method to create from decimal representation (0.25 = 25%).
    /// </summary>
    public static Percentage FromDecimal(decimal decimalValue)
    {
        if (decimalValue < 0 || decimalValue > 1)
            throw new ArgumentException("Decimal value must be between 0 and 1", nameof(decimalValue));

        return new Percentage(decimalValue * 100);
    }

    /// <summary>
    /// Factory method to create from percentage value string ("25" or "25%").
    /// </summary>
    public static Percentage Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty", nameof(value));

        var trimmed = value.Trim().TrimEnd('%');
        if (decimal.TryParse(trimmed, out var percentValue))
        {
            return new Percentage(percentValue);
        }

        throw new ArgumentException($"Invalid percentage value: {value}", nameof(value));
    }

    /// <summary>
    /// Check if percentage is zero.
    /// </summary>
    public bool IsZero => Value == 0;

    /// <summary>
    /// Format percentage as string.
    /// </summary>
    public override string ToString() => $"{Value:N2}%";
}
