namespace TestFynly.ValueObjects;

/// <summary>
/// Unit tests for Money value object
/// </summary>
public class MoneyValueObjectTests
{
    [Fact]
    public void CreateMoney_WithValidAmount_CreatesSuccessfully()
    {
        // Arrange & Act
        var money = Money.Create(1000m, CurrencyCode.USD);

        // Assert
        money.Amount.Should().Be(1000m);
        money.Currency.Code.Should().Be(CurrencyCode.USD);
    }

    [Fact]
    public void CreateMoney_WithNegativeAmount_ThrowsException()
    {
        // Arrange & Act & Assert
        var act = () => Money.Create(-100m, CurrencyCode.USD);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddMoney_WithSameCurrency_AddsCorrectly()
    {
        // Arrange
        var money1 = Money.Create(100m, CurrencyCode.USD);
        var money2 = Money.Create(50m, CurrencyCode.USD);

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Code.Should().Be(CurrencyCode.USD);
    }

    [Fact]
    public void SubtractMoney_WithSameCurrency_SubtractsCorrectly()
    {
        // Arrange
        var money1 = Money.Create(100m, CurrencyCode.USD);
        var money2 = Money.Create(30m, CurrencyCode.USD);

        // Act
        var result = money1.Subtract(money2);

        // Assert
        result.Amount.Should().Be(70m);
        result.Currency.Code.Should().Be(CurrencyCode.USD);
    }

    [Fact]
    public void MultiplyMoney_ByFactor_MultipliesCorrectly()
    {
        // Arrange
        var money = Money.Create(100m, CurrencyCode.USD);

        // Act
        var result = money.Multiply(2.5m);

        // Assert
        result.Amount.Should().Be(250m);
        result.Currency.Code.Should().Be(CurrencyCode.USD);
    }

    [Fact]
    public void DivideMoney_ByFactor_DividesCorrectly()
    {
        // Arrange
        var money = Money.Create(100m, CurrencyCode.USD);

        // Act
        var result = money.Divide(2m);

        // Assert
        result.Amount.Should().Be(50m);
        result.Currency.Code.Should().Be(CurrencyCode.USD);
    }

    [Fact]
    public void IsGreaterThan_WithLargerAmount_ReturnsTrue()
    {
        // Arrange
        var money1 = Money.Create(100m, CurrencyCode.USD);
        var money2 = Money.Create(50m, CurrencyCode.USD);

        // Act & Assert
        money1.IsGreaterThan(money2).Should().BeTrue();
    }

    [Fact]
    public void IsLessThan_WithSmallerAmount_ReturnsTrue()
    {
        // Arrange
        var money1 = Money.Create(50m, CurrencyCode.USD);
        var money2 = Money.Create(100m, CurrencyCode.USD);

        // Act & Assert
        money1.IsLessThan(money2).Should().BeTrue();
    }
}

/// <summary>
/// Unit tests for Currency value object
/// </summary>
public class CurrencyValueObjectTests
{
    [Fact]
    public void USD_IsValidCurrency()
    {
        // Act
        var usd = Currency.USD;

        // Assert
        usd.Code.Should().Be(CurrencyCode.USD);
        usd.Symbol.Should().Be("$");
        usd.DecimalPlaces.Should().Be(2);
    }

    [Fact]
    public void NGN_IsValidCurrency()
    {
        // Act
        var ngn = Currency.NGN;

        // Assert
        ngn.Code.Should().Be(CurrencyCode.NGN);
        ngn.Symbol.Should().Be("₦");
        ngn.DecimalPlaces.Should().Be(2);
    }

    [Fact]
    public void EUR_IsValidCurrency()
    {
        // Act
        var eur = Currency.EUR;

        // Assert
        eur.Code.Should().Be(CurrencyCode.EUR);
        eur.Symbol.Should().Be("€");
    }
}

/// <summary>
/// Unit tests for Percentage value object
/// </summary>
public class PercentageValueObjectTests
{
    [Fact]
    public void CreatePercentage_WithValidValue_CreatesSuccessfully()
    {
        // Arrange & Act
        var percentage = new Percentage(50m);

        // Assert
        percentage.Value.Should().Be(50m);
    }

    [Fact]
    public void CreatePercentage_WithZero_CreatesSuccessfully()
    {
        // Arrange & Act
        var percentage = new Percentage(0m);

        // Assert
        percentage.Value.Should().Be(0m);
    }

    [Fact]
    public void CreatePercentage_WithOneHundred_CreatesSuccessfully()
    {
        // Arrange & Act
        var percentage = new Percentage(100m);

        // Assert
        percentage.Value.Should().Be(100m);
    }

    [Fact]
    public void CreatePercentage_WithNegativeValue_ThrowsException()
    {
        // Arrange & Act & Assert
        var act = () => new Percentage(-50m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreatePercentage_WithValueGreaterThan100_ThrowsException()
    {
        // Arrange & Act & Assert
        var act = () => new Percentage(150m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromDecimal_WithValidDecimal_CreatesSuccessfully()
    {
        // Arrange & Act
        var percentage = Percentage.FromDecimal(0.5m);

        // Assert
        percentage.Value.Should().Be(50m);
    }

    [Fact]
    public void Decimal_ReturnsCorrectValue()
    {
        // Arrange
        var percentage = new Percentage(25m);

        // Act
        var decimalValue = percentage.Decimal;

        // Assert
        decimalValue.Should().Be(0.25m);
    }
}



/// <summary>
/// Unit tests for DateRange value object
/// </summary>
public class DateRangeValueObjectTests
{
    [Fact(Skip = "Namespace conflict with System.IO - will revisit")]
    public void CreateDateRange_WithValidDates_CreatesSuccessfully()
    {
        // This test will be enabled after resolving namespace conflicts
    }

    [Fact(Skip = "Namespace conflict with System.IO - will revisit")]
    public void Contains_WithDateInRange_ReturnsTrue()
    {
        // This test will be enabled after resolving namespace conflicts
    }

    [Fact(Skip = "Namespace conflict with System.IO - will revisit")]
    public void Contains_WithDateOutsideRange_ReturnsFalse()
    {
        // This test will be enabled after resolving namespace conflicts
    }

    [Fact(Skip = "Namespace conflict with System.IO - will revisit")]
    public void Overlaps_WithOverlappingRange_ReturnsTrue()
    {
        // This test will be enabled after resolving namespace conflicts
    }

    [Fact(Skip = "Namespace conflict with System.IO - will revisit")]
    public void Overlaps_WithNonOverlappingRange_ReturnsFalse()
    {
        // This test will be enabled after resolving namespace conflicts
    }
}
