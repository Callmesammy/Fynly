namespace AiCFO.Domain.Entities;

/// <summary>
/// AccountBalance - Tracks the balance of an account for reporting and queries.
/// Maintains both current balance and period balances for efficiency.
/// </summary>
public class AccountBalance : Entity
{
    public Guid TenantId { get; private set; }
    public AccountCode AccountCode { get; private set; }
    public DateTime BalanceDate { get; private set; }
    public Money CurrentBalance { get; private set; }
    public Money DebitBalance { get; private set; }
    public Money CreditBalance { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public Guid LastUpdatedBy { get; private set; }

    // Period tracking (for reporting)
    public int FiscalYear { get; private set; }
    public int FiscalPeriod { get; private set; }

    // EF Core constructor
    protected AccountBalance() { }

    public AccountBalance(
        Guid id,
        Guid tenantId,
        AccountCode accountCode,
        DateTime balanceDate,
        int fiscalYear,
        int fiscalPeriod,
        Guid createdBy)
    {
        Id = id;
        TenantId = tenantId;
        AccountCode = accountCode ?? throw new ArgumentNullException(nameof(accountCode));
        BalanceDate = balanceDate;
        FiscalYear = fiscalYear;
        FiscalPeriod = fiscalPeriod;
        CurrentBalance = Money.Create(0m, CurrencyCode.USD);
        DebitBalance = Money.Create(0m, CurrencyCode.USD);
        CreditBalance = Money.Create(0m, CurrencyCode.USD);
        LastUpdated = DateTime.UtcNow;
        LastUpdatedBy = createdBy;
    }

    /// <summary>
    /// Apply a debit to this account balance.
    /// </summary>
    public void ApplyDebit(Money amount, Guid appliedBy)
    {
        if (amount.Amount < 0)
            throw new ArgumentException("Debit amount cannot be negative", nameof(amount));

        var newDebitBalance = DebitBalance.Add(amount);
        var newCurrentBalance = CalculateBalance(newDebitBalance, CreditBalance);

        DebitBalance = newDebitBalance;
        CurrentBalance = newCurrentBalance;
        LastUpdated = DateTime.UtcNow;
        LastUpdatedBy = appliedBy;
    }

    /// <summary>
    /// Apply a credit to this account balance.
    /// </summary>
    public void ApplyCredit(Money amount, Guid appliedBy)
    {
        if (amount.Amount < 0)
            throw new ArgumentException("Credit amount cannot be negative", nameof(amount));

        var newCreditBalance = CreditBalance.Add(amount);
        var newCurrentBalance = CalculateBalance(DebitBalance, newCreditBalance);

        CreditBalance = newCreditBalance;
        CurrentBalance = newCurrentBalance;
        LastUpdated = DateTime.UtcNow;
        LastUpdatedBy = appliedBy;
    }

    /// <summary>
    /// Reverse a transaction (undo debit or credit).
    /// </summary>
    public void ReverseDebit(Money amount, Guid reversedBy)
    {
        if (amount.Amount < 0 || amount.Amount > DebitBalance.Amount)
            throw new ArgumentException("Invalid debit reversal amount", nameof(amount));

        DebitBalance = DebitBalance.Subtract(amount);
        CurrentBalance = CalculateBalance(DebitBalance, CreditBalance);
        LastUpdated = DateTime.UtcNow;
        LastUpdatedBy = reversedBy;
    }

    public void ReverseCredit(Money amount, Guid reversedBy)
    {
        if (amount.Amount < 0 || amount.Amount > CreditBalance.Amount)
            throw new ArgumentException("Invalid credit reversal amount", nameof(amount));

        CreditBalance = CreditBalance.Subtract(amount);
        CurrentBalance = CalculateBalance(DebitBalance, CreditBalance);
        LastUpdated = DateTime.UtcNow;
        LastUpdatedBy = reversedBy;
    }

    /// <summary>
    /// Calculate current balance based on account type.
    /// Assets and Expenses: Balance = Debits - Credits
    /// Liabilities, Equity, Income: Balance = Credits - Debits
    /// </summary>
    private Money CalculateBalance(Money debits, Money credits)
    {
        // For now, return the difference (assume asset account)
        // This will be enhanced when we have access to the account type
        var netAmount = debits.Amount - credits.Amount;
        var absAmount = Math.Abs(netAmount);
        return Money.Create(absAmount, debits.Currency.Code);
    }

    /// <summary>
    /// Reset balance to zero (for period closing).
    /// </summary>
    public void Reset(Guid resetBy)
    {
        DebitBalance = Money.Create(0m, DebitBalance.Currency.Code);
        CreditBalance = Money.Create(0m, CreditBalance.Currency.Code);
        CurrentBalance = Money.Create(0m, CurrentBalance.Currency.Code);
        LastUpdated = DateTime.UtcNow;
        LastUpdatedBy = resetBy;
    }
}
