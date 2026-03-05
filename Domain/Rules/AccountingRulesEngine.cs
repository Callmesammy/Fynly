namespace AiCFO.Domain.Rules;

/// <summary>
/// Accounting Rules Engine - Orchestrates validation of all accounting rules.
/// Collects rule violations and provides comprehensive error reporting.
/// </summary>
public class AccountingRulesEngine
{
    private readonly List<IAccountingRule> _rules = new();
    private readonly List<string> _violations = new();

    /// <summary>
    /// Add a rule to be validated.
    /// </summary>
    public void AddRule(IAccountingRule rule)
    {
        if (rule != null)
        {
            _rules.Add(rule);
        }
    }

    /// <summary>
    /// Add multiple rules at once.
    /// </summary>
    public void AddRules(params IAccountingRule[] rules)
    {
        foreach (var rule in rules.Where(r => r != null))
        {
            _rules.Add(rule);
        }
    }

    /// <summary>
    /// Execute all registered rules and collect violations.
    /// </summary>
    public void Validate()
    {
        _violations.Clear();

        foreach (var rule in _rules)
        {
            var violation = rule.Validate();
            if (!string.IsNullOrWhiteSpace(violation))
            {
                _violations.Add($"[{rule.RuleName}] {violation}");
            }
        }
    }

    /// <summary>
    /// Check if all rules passed validation.
    /// </summary>
    public bool IsValid => _violations.Count == 0;

    /// <summary>
    /// Get all validation violations.
    /// </summary>
    public IReadOnlyList<string> Violations => _violations.AsReadOnly();

    /// <summary>
    /// Get consolidated error message.
    /// </summary>
    public string GetErrorMessage()
    {
        if (IsValid)
            return string.Empty;

        return string.Join("\n", _violations);
    }

    /// <summary>
    /// Clear all rules and violations (for reuse).
    /// </summary>
    public void Clear()
    {
        _rules.Clear();
        _violations.Clear();
    }
}

/// <summary>
/// Builder pattern for constructing and validating accounting operations.
/// Fluent API for rule specification.
/// </summary>
public class AccountingRulesBuilder
{
    private readonly AccountingRulesEngine _engine = new();

    /// <summary>
    /// Add double-entry validation rule.
    /// </summary>
    public AccountingRulesBuilder WithDoubleEntry(decimal totalDebits, decimal totalCredits)
    {
        _engine.AddRule(new DoubleEntryRule(totalDebits, totalCredits));
        return this;
    }

    /// <summary>
    /// Add debit/credit balance rule.
    /// </summary>
    public AccountingRulesBuilder WithBalanceRule(AccountType accountType, Money balance)
    {
        _engine.AddRule(new DebitCreditBalanceRule(accountType, balance));
        return this;
    }

    /// <summary>
    /// Add account type rule.
    /// </summary>
    public AccountingRulesBuilder WithAccountTypeRule(AccountType expectedType, AccountType actualType)
    {
        _engine.AddRule(new AccountTypeRule(expectedType, actualType));
        return this;
    }

    /// <summary>
    /// Add transaction line rule.
    /// </summary>
    public AccountingRulesBuilder WithTransactionLineRule(Money amount, string description)
    {
        _engine.AddRule(new TransactionLineRule(amount, description));
        return this;
    }

    /// <summary>
    /// Add account balance constraint rule.
    /// </summary>
    public AccountingRulesBuilder WithBalanceConstraint(AccountCode accountCode, Money balance, Money? minimum = null, Money? maximum = null)
    {
        _engine.AddRule(new AccountBalanceConstraintRule(accountCode, balance, minimum, maximum));
        return this;
    }

    /// <summary>
    /// Add custom rule.
    /// </summary>
    public AccountingRulesBuilder WithCustomRule(IAccountingRule rule)
    {
        _engine.AddRule(rule);
        return this;
    }

    /// <summary>
    /// Execute validation and get result.
    /// </summary>
    public AccountingValidationResult Build()
    {
        _engine.Validate();
        return new AccountingValidationResult(_engine.IsValid, _engine.Violations, _engine.GetErrorMessage());
    }
}

/// <summary>
/// Result of accounting validation.
/// </summary>
public class AccountingValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Violations { get; }
    public string ErrorMessage { get; }

    internal AccountingValidationResult(bool isValid, IReadOnlyList<string> violations, string errorMessage)
    {
        IsValid = isValid;
        Violations = violations;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Create a successful validation result.
    /// </summary>
    public static AccountingValidationResult Success() => new(true, new List<string>(), string.Empty);

    /// <summary>
    /// Create a failed validation result with error message.
    /// </summary>
    public static AccountingValidationResult Failure(string errorMessage) => new(false, new List<string> { errorMessage }, errorMessage);
}
