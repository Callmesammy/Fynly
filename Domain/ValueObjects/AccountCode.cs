namespace AiCFO.Domain.ValueObjects;

/// <summary>
/// Account type enumeration for accounting classification.
/// Follows standard accounting hierarchy: Assets, Liabilities, Equity, Income, Expenses.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Assets - Resources owned by the business (debit balance increases, credit balance decreases).
    /// </summary>
    Asset = 1,

    /// <summary>
    /// Liabilities - Obligations owed by the business (credit balance increases, debit balance decreases).
    /// </summary>
    Liability = 2,

    /// <summary>
    /// Equity - Owner's interest in the business (credit balance increases, debit balance decreases).
    /// </summary>
    Equity = 3,

    /// <summary>
    /// Income/Revenue - Earnings from business operations (credit balance increases, debit balance decreases).
    /// </summary>
    Income = 4,

    /// <summary>
    /// Expenses - Costs incurred in business operations (debit balance increases, credit balance decreases).
    /// </summary>
    Expense = 5,
}

/// <summary>
/// Account sub-type for more detailed classification within account types.
/// </summary>
public enum AccountSubType
{
    // Asset sub-types
    CurrentAsset = 1,
    FixedAsset = 2,
    Receivable = 3,
    CashEquivalent = 4,

    // Liability sub-types
    CurrentLiability = 10,
    LongTermLiability = 11,
    Payable = 12,

    // Equity sub-types
    ShareCapital = 20,
    RetainedEarnings = 21,
    Reserve = 22,

    // Income sub-types
    OperatingIncome = 30,
    NonOperatingIncome = 31,
    OtherIncome = 32,

    // Expense sub-types
    OperatingExpense = 40,
    CostOfGoodsSold = 41,
    NonOperatingExpense = 42,
    OtherExpense = 43,
}

/// <summary>
/// Account code structure: Hierarchical account numbering system.
/// Format: XXXXX (5-digit code)
/// - First digit: Account type (1=Asset, 2=Liability, 3=Equity, 4=Income, 5=Expense)
/// - Next digits: Subcategories and specific account
/// </summary>
public sealed record AccountCode
{
    public string Code { get; }

    public AccountCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Account code cannot be empty", nameof(code));

        if (!code.All(char.IsDigit) || code.Length != 5)
            throw new ArgumentException("Account code must be exactly 5 digits", nameof(code));

        Code = code;
    }

    /// <summary>
    /// Parse account code from string.
    /// </summary>
    public static AccountCode Parse(string code) => new AccountCode(code);

    /// <summary>
    /// Factory method to create account code for common account types.
    /// </summary>
    public static AccountCode CreateAsset(string subCode) => new AccountCode($"1{subCode}");
    public static AccountCode CreateLiability(string subCode) => new AccountCode($"2{subCode}");
    public static AccountCode CreateEquity(string subCode) => new AccountCode($"3{subCode}");
    public static AccountCode CreateIncome(string subCode) => new AccountCode($"4{subCode}");
    public static AccountCode CreateExpense(string subCode) => new AccountCode($"5{subCode}");

    public override string ToString() => Code;
}

/// <summary>
/// Account value object - represents a chart of accounts entry.
/// </summary>
public sealed record Account
{
    public AccountCode Code { get; }
    public string Name { get; }
    public string? Description { get; }
    public AccountType Type { get; }
    public AccountSubType SubType { get; }

    public Account(AccountCode code, string name, AccountType type, AccountSubType subType, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty", nameof(name));

        Code = code ?? throw new ArgumentNullException(nameof(code));
        Name = name;
        Type = type;
        SubType = subType;
        Description = description;
    }

    /// <summary>
    /// Determines if this account is a debit or credit balance account.
    /// Assets and Expenses increase with debits.
    /// Liabilities, Equity, and Income increase with credits.
    /// </summary>
    public bool IsDebitBalanceAccount => Type is AccountType.Asset or AccountType.Expense;

    /// <summary>
    /// Get the normal balance direction for this account.
    /// </summary>
    public string NormalBalance => IsDebitBalanceAccount ? "Debit" : "Credit";
}
