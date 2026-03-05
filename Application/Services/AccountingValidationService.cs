namespace AiCFO.Application.Services;

using AiCFO.Domain.Rules;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Service for validating accounting operations against business rules.
/// Uses the accounting rules engine to ensure data integrity.
/// </summary>
public interface IAccountingValidationService
{
    /// <summary>
    /// Validate a journal entry before posting.
    /// </summary>
    AccountingValidationResult ValidateJournalEntry(Money totalDebits, Money totalCredits, int lineCount);

    /// <summary>
    /// Validate account balance is within constraints.
    /// </summary>
    AccountingValidationResult ValidateAccountBalance(AccountCode accountCode, Money balance, Money? minBalance = null, Money? maxBalance = null);

    /// <summary>
    /// Validate a transaction line.
    /// </summary>
    AccountingValidationResult ValidateTransactionLine(Money amount, string description);

    /// <summary>
    /// Validate account type consistency.
    /// </summary>
    AccountingValidationResult ValidateAccountType(AccountType expectedType, AccountType actualType);
}

/// <summary>
/// Implementation of accounting validation service.
/// </summary>
public class AccountingValidationService : IAccountingValidationService
{
    public AccountingValidationResult ValidateJournalEntry(Money totalDebits, Money totalCredits, int lineCount)
    {
        var result = new AccountingRulesBuilder()
            .WithDoubleEntry(totalDebits.Amount, totalCredits.Amount)
            .Build();

        if (!result.IsValid)
            return result;

        if (lineCount == 0)
            return AccountingValidationResult.Failure("Journal entry must have at least one line");

        return result;
    }

    public AccountingValidationResult ValidateAccountBalance(AccountCode accountCode, Money balance, Money? minBalance = null, Money? maxBalance = null)
    {
        return new AccountingRulesBuilder()
            .WithBalanceConstraint(accountCode, balance, minBalance, maxBalance)
            .Build();
    }

    public AccountingValidationResult ValidateTransactionLine(Money amount, string description)
    {
        return new AccountingRulesBuilder()
            .WithTransactionLineRule(amount, description)
            .Build();
    }

    public AccountingValidationResult ValidateAccountType(AccountType expectedType, AccountType actualType)
    {
        return new AccountingRulesBuilder()
            .WithAccountTypeRule(expectedType, actualType)
            .Build();
    }
}
