namespace AiCFO.Domain.Rules;

using AiCFO.Domain.ValueObjects;

/// <summary>
/// Base abstraction for accounting rules.
/// All accounting rules must implement this interface to participate in validation.
/// </summary>
public interface IAccountingRule
{
    /// <summary>
    /// Gets the rule name/description.
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Validates the rule. Returns null if valid, error message if invalid.
    /// </summary>
    string? Validate();
}

/// <summary>
/// Double-entry accounting validation rule.
/// Core principle: Total debits must equal total credits.
/// </summary>
public class DoubleEntryRule : IAccountingRule
{
    public string RuleName => "Double-Entry Accounting Principle";
    
    private readonly decimal _totalDebits;
    private readonly decimal _totalCredits;
    private readonly decimal _tolerance;

    public DoubleEntryRule(decimal totalDebits, decimal totalCredits, decimal tolerance = 0.01m)
    {
        _totalDebits = totalDebits;
        _totalCredits = totalCredits;
        _tolerance = tolerance;
    }

    public string? Validate()
    {
        var difference = Math.Abs(_totalDebits - _totalCredits);
        
        if (difference > _tolerance)
        {
            return $"Entry does not balance. Debits: {_totalDebits:C}, Credits: {_totalCredits:C}, Difference: {difference:C}";
        }

        return null;
    }
}

/// <summary>
/// Debit/credit balance rule for specific account types.
/// Different account types have different debit/credit behaviors.
/// </summary>
public class DebitCreditBalanceRule : IAccountingRule
{
    public string RuleName => "Debit/Credit Balance Rule";
    
    private readonly AccountType _accountType;
    private readonly Money _balance;

    public DebitCreditBalanceRule(AccountType accountType, Money balance)
    {
        _accountType = accountType;
        _balance = balance;
    }

    public string? Validate()
    {
        // Assets, Expenses: Should have debit balance (positive = good)
        // Liabilities, Equity, Income: Should have credit balance (positive = good)
        
        var isBalanceValid = _accountType switch
        {
            AccountType.Asset => _balance.Amount >= 0,
            AccountType.Liability => _balance.Amount >= 0, // Credit balance means positive
            AccountType.Equity => _balance.Amount >= 0, // Credit balance means positive
            AccountType.Income => _balance.Amount >= 0, // Credit balance means positive
            AccountType.Expense => _balance.Amount >= 0, // Debit balance means positive
            _ => true
        };

        if (!isBalanceValid)
        {
            return $"{_accountType} account should maintain a positive balance. Current balance: {_balance.Amount:C}";
        }

        return null;
    }
}

/// <summary>
/// Account type rule: Ensures accounts are classified correctly.
/// Validates that account operations are consistent with account type.
/// </summary>
public class AccountTypeRule : IAccountingRule
{
    public string RuleName => "Account Type Classification Rule";
    
    private readonly AccountType _expectedType;
    private readonly AccountType _actualType;

    public AccountTypeRule(AccountType expectedType, AccountType actualType)
    {
        _expectedType = expectedType;
        _actualType = actualType;
    }

    public string? Validate()
    {
        if (_expectedType != _actualType)
        {
            return $"Account type mismatch. Expected: {_expectedType}, Got: {_actualType}";
        }

        return null;
    }
}

/// <summary>
/// Transaction line rule: Validates individual line items in a journal entry.
/// </summary>
public class TransactionLineRule : IAccountingRule
{
    public string RuleName => "Transaction Line Validation Rule";
    
    private readonly Money _amount;
    private readonly string _description;

    public TransactionLineRule(Money amount, string description)
    {
        _amount = amount;
        _description = description;
    }

    public string? Validate()
    {
        if (_amount.Amount <= 0)
        {
            return "Transaction line amount must be greater than zero";
        }

        if (string.IsNullOrWhiteSpace(_description))
        {
            return "Transaction line description cannot be empty";
        }

        return null;
    }
}

/// <summary>
/// Account balance constraint: Ensures balances don't violate business rules.
/// Example: Prevent negative cash accounts.
/// </summary>
public class AccountBalanceConstraintRule : IAccountingRule
{
    public string RuleName => "Account Balance Constraint Rule";
    
    private readonly AccountCode _accountCode;
    private readonly Money _balance;
    private readonly Money? _minimumBalance;
    private readonly Money? _maximumBalance;

    public AccountBalanceConstraintRule(AccountCode accountCode, Money balance, Money? minimumBalance = null, Money? maximumBalance = null)
    {
        _accountCode = accountCode;
        _balance = balance;
        _minimumBalance = minimumBalance;
        _maximumBalance = maximumBalance;
    }

    public string? Validate()
    {
        if (_minimumBalance != null && _balance.Amount < _minimumBalance.Amount)
        {
            return $"Account {_accountCode} balance cannot go below {_minimumBalance.Amount:C}. Current: {_balance.Amount:C}";
        }

        if (_maximumBalance != null && _balance.Amount > _maximumBalance.Amount)
        {
            return $"Account {_accountCode} balance cannot exceed {_maximumBalance.Amount:C}. Current: {_balance.Amount:C}";
        }

        return null;
    }
}
