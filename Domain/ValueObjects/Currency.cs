namespace AiCFO.Domain.ValueObjects;

/// <summary>
/// Supported currencies for the platform.
/// </summary>
public enum CurrencyCode
{
    NGN,  // Nigerian Naira
    USD,  // US Dollar
    EUR,  // Euro
    GBP,  // British Pound
    KES,  // Kenyan Shilling
    GHS,  // Ghanaian Cedi
    ZAR,  // South African Rand
    CAD,  // Canadian Dollar
    AUD,  // Australian Dollar
    JPY,  // Japanese Yen
    INR,  // Indian Rupee
    CNY,  // Chinese Yuan
}

/// <summary>
/// Currency value object - immutable representation of a currency.
/// </summary>
public sealed record Currency
{
    public CurrencyCode Code { get; }
    public string Symbol { get; }
    public int DecimalPlaces { get; }
    public string Name { get; }

    public static readonly Currency NGN = new(CurrencyCode.NGN, "₦", 2, "Nigerian Naira");
    public static readonly Currency USD = new(CurrencyCode.USD, "$", 2, "US Dollar");
    public static readonly Currency EUR = new(CurrencyCode.EUR, "€", 2, "Euro");
    public static readonly Currency GBP = new(CurrencyCode.GBP, "£", 2, "British Pound");
    public static readonly Currency KES = new(CurrencyCode.KES, "KSh", 2, "Kenyan Shilling");
    public static readonly Currency GHS = new(CurrencyCode.GHS, "GH₵", 2, "Ghanaian Cedi");
    public static readonly Currency ZAR = new(CurrencyCode.ZAR, "R", 2, "South African Rand");
    public static readonly Currency CAD = new(CurrencyCode.CAD, "C$", 2, "Canadian Dollar");
    public static readonly Currency AUD = new(CurrencyCode.AUD, "A$", 2, "Australian Dollar");
    public static readonly Currency JPY = new(CurrencyCode.JPY, "¥", 0, "Japanese Yen");
    public static readonly Currency INR = new(CurrencyCode.INR, "₹", 2, "Indian Rupee");
    public static readonly Currency CNY = new(CurrencyCode.CNY, "¥", 2, "Chinese Yuan");

    private Currency(CurrencyCode code, string symbol, int decimalPlaces, string name)
    {
        Code = code;
        Symbol = symbol;
        DecimalPlaces = decimalPlaces;
        Name = name;
    }

    public static Currency FromCode(CurrencyCode code) =>
        code switch
        {
            CurrencyCode.NGN => NGN,
            CurrencyCode.USD => USD,
            CurrencyCode.EUR => EUR,
            CurrencyCode.GBP => GBP,
            CurrencyCode.KES => KES,
            CurrencyCode.GHS => GHS,
            CurrencyCode.ZAR => ZAR,
            CurrencyCode.CAD => CAD,
            CurrencyCode.AUD => AUD,
            CurrencyCode.JPY => JPY,
            CurrencyCode.INR => INR,
            CurrencyCode.CNY => CNY,
            _ => throw new ArgumentException($"Unsupported currency code: {code}")
        };

    public static Currency FromString(string code) =>
        Enum.TryParse<CurrencyCode>(code, ignoreCase: true, out var currencyCode)
            ? FromCode(currencyCode)
            : throw new ArgumentException($"Invalid currency code: {code}");

    public override string ToString() => Code.ToString();
}
