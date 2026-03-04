namespace AiCFO.Domain.ValueObjects;

/// <summary>
/// DateRange value object - immutable representation of a date range.
/// Ensures StartDate is always less than or equal to EndDate.
/// </summary>
public sealed record DateRange
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException(
                $"StartDate ({startDate:yyyy-MM-dd}) cannot be after EndDate ({endDate:yyyy-MM-dd})",
                nameof(startDate));

        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    /// Get the number of days in the date range (inclusive).
    /// </summary>
    public int Days => (EndDate - StartDate).Days + 1;

    /// <summary>
    /// Check if a date falls within this range (inclusive).
    /// </summary>
    public bool Contains(DateTime date)
    {
        return date >= StartDate && date <= EndDate;
    }

    /// <summary>
    /// Check if another date range overlaps with this range.
    /// </summary>
    public bool Overlaps(DateRange other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        return StartDate <= other.EndDate && EndDate >= other.StartDate;
    }

    /// <summary>
    /// Check if another date range is completely contained within this range.
    /// </summary>
    public bool Contains(DateRange other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        return StartDate <= other.StartDate && EndDate >= other.EndDate;
    }

    /// <summary>
    /// Get intersection of this range with another range.
    /// Returns null if ranges don't overlap.
    /// </summary>
    public DateRange? GetIntersection(DateRange other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        if (!Overlaps(other))
            return null;

        var intersectStart = StartDate > other.StartDate ? StartDate : other.StartDate;
        var intersectEnd = EndDate < other.EndDate ? EndDate : other.EndDate;

        return new DateRange(intersectStart, intersectEnd);
    }

    /// <summary>
    /// Factory method to create a range for the current month.
    /// </summary>
    public static DateRange CurrentMonth()
    {
        var today = DateTime.UtcNow;
        var firstDay = new DateTime(today.Year, today.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        return new DateRange(firstDay, lastDay);
    }

    /// <summary>
    /// Factory method to create a range for the current quarter.
    /// </summary>
    public static DateRange CurrentQuarter()
    {
        var today = DateTime.UtcNow;
        var quarter = (today.Month - 1) / 3 + 1;
        var startMonth = (quarter - 1) * 3 + 1;
        var firstDay = new DateTime(today.Year, startMonth, 1);
        var lastDay = firstDay.AddMonths(3).AddDays(-1);

        return new DateRange(firstDay, lastDay);
    }

    /// <summary>
    /// Factory method to create a range for the current year.
    /// </summary>
    public static DateRange CurrentYear()
    {
        var today = DateTime.UtcNow;
        var firstDay = new DateTime(today.Year, 1, 1);
        var lastDay = new DateTime(today.Year, 12, 31);

        return new DateRange(firstDay, lastDay);
    }

    /// <summary>
    /// Factory method to create a range for the last N days.
    /// </summary>
    public static DateRange LastDays(int days)
    {
        if (days <= 0)
            throw new ArgumentException("Days must be positive", nameof(days));

        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-days + 1);

        return new DateRange(startDate, endDate);
    }

    /// <summary>
    /// Format date range as string (e.g., "2025-01-01 to 2025-12-31").
    /// </summary>
    public override string ToString()
    {
        return $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";
    }
}
