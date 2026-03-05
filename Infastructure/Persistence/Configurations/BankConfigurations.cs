namespace AiCFO.Infrastructure.Persistence.Configurations;

using AiCFO.Domain.Entities;
using AiCFO.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core configurations for bank-related entities.
/// </summary>

/// <summary>
/// Configuration for BankConnection aggregate root.
/// </summary>
public class BankConnectionConfiguration : IEntityTypeConfiguration<BankConnection>
{
    public void Configure(EntityTypeBuilder<BankConnection> builder)
    {
        builder.ToTable("BankConnections");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.TenantId).IsRequired().HasMaxLength(36);
        builder.Property(b => b.Provider).IsRequired().HasConversion<int>();
        builder.Property(b => b.BankCode).IsRequired();
        builder.Property(b => b.BankName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Status).IsRequired().HasConversion<int>();
        builder.Property(b => b.AccessToken).HasMaxLength(1000);
        builder.Property(b => b.TokenExpiresAt);
        builder.Property(b => b.LastSyncAt);
        builder.Property(b => b.SyncError).HasMaxLength(500);
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.CreatedBy).IsRequired();
        builder.Property(b => b.UpdatedAt);
        builder.Property(b => b.UpdatedBy);

        builder.HasIndex(b => b.TenantId).HasDatabaseName("IX_BankConnections_TenantId");
        builder.HasIndex(b => b.Provider).HasDatabaseName("IX_BankConnections_Provider");
        builder.HasIndex(b => new { b.TenantId, b.Status }).HasDatabaseName("IX_BankConnections_TenantId_Status");

        // Relationship to bank accounts
        builder.HasMany(b => b.Accounts)
            .WithOne()
            .HasForeignKey(a => a.BankConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for BankAccount entity.
/// </summary>
public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.BankConnectionId).IsRequired();
        builder.Property(a => a.BankAccountId).IsRequired();
        builder.Property(a => a.AccountNumber).IsRequired().HasMaxLength(50);
        builder.Property(a => a.AccountName).IsRequired().HasMaxLength(200);
        builder.OwnsOne(a => a.Currency, c => c.ToJson());
        builder.OwnsOne(a => a.CurrentBalance, cb =>
        {
            cb.Property(m => m.Amount).HasColumnName("CurrentBalance_Amount").HasPrecision(18, 2);
            cb.Property(m => m.Currency.Code).HasColumnName("CurrentBalance_CurrencyCode").HasConversion<string>().HasMaxLength(3);
        });
        builder.Property(a => a.LastBalanceUpdate);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.CreatedBy).IsRequired();
        builder.Property(a => a.UpdatedAt);
        builder.Property(a => a.UpdatedBy);
        builder.Property(a => a.IsArchived).IsRequired().HasDefaultValue(false);

        builder.HasIndex(a => a.BankConnectionId).HasDatabaseName("IX_BankAccounts_BankConnectionId");
        builder.HasIndex(a => new { a.BankConnectionId, a.IsArchived }).HasDatabaseName("IX_BankAccounts_BankConnectionId_IsArchived");

        // Relationship to transactions
        builder.HasMany(a => a.Transactions)
            .WithOne()
            .HasForeignKey(t => t.BankAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configuration for BankTransaction entity.
/// </summary>
public class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.ToTable("BankTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.BankAccountId).IsRequired();
        builder.Property(t => t.BankTransactionId).IsRequired().HasMaxLength(100);
        builder.Property(t => t.TransactionDate).IsRequired();
        builder.OwnsOne(t => t.Amount, a =>
        {
            a.Property(m => m.Amount).HasColumnName("Amount_Value").HasPrecision(18, 2);
            a.Property(m => m.Currency.Code).HasColumnName("Amount_CurrencyCode").HasConversion<string>().HasMaxLength(3);
        });
        builder.Property(t => t.TransactionType).IsRequired().HasConversion<int>();
        builder.Property(t => t.Description).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Reference).HasMaxLength(100);
        builder.Property(t => t.CounterpartyName).HasMaxLength(200);
        builder.Property(t => t.CounterpartyAccount).HasMaxLength(50);
        builder.Property(t => t.LinkedJournalLineId);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt);

        builder.HasIndex(t => t.BankAccountId).HasDatabaseName("IX_BankTransactions_BankAccountId");
        builder.HasIndex(t => t.BankTransactionId).IsUnique().HasDatabaseName("IX_BankTransactions_BankTransactionId_Unique");
        builder.HasIndex(t => t.TransactionDate).HasDatabaseName("IX_BankTransactions_TransactionDate");
        builder.HasIndex(t => t.LinkedJournalLineId).HasDatabaseName("IX_BankTransactions_LinkedJournalLineId");
        builder.HasIndex(t => new { t.BankAccountId, t.LinkedJournalLineId }).HasDatabaseName("IX_BankTransactions_AccountId_LinkedJournalLine");
    }
}
