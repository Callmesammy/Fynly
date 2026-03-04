namespace AiCFO.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core configuration for ChartOfAccounts aggregate root.
/// </summary>
public class ChartOfAccountsConfiguration : IEntityTypeConfiguration<ChartOfAccounts>
{
    public void Configure(EntityTypeBuilder<ChartOfAccounts> builder)
    {
        builder.ToTable("ChartOfAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CompanyName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.Property(x => x.UpdatedBy)
            .IsRequired(false);

        builder.Property(x => x.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        // Navigation to accounts
        builder.HasMany<ChartAccountEntry>()
            .WithOne(x => x.ChartOfAccounts)
            .HasForeignKey(x => x.ChartOfAccountsId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indices
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.IsArchived });
    }
}

/// <summary>
/// EF Core configuration for ChartAccountEntry entity.
/// </summary>
public class ChartAccountEntryConfiguration : IEntityTypeConfiguration<ChartAccountEntry>
{
    public void Configure(EntityTypeBuilder<ChartAccountEntry> builder)
    {
        builder.ToTable("ChartAccountEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChartOfAccountsId)
            .IsRequired();

        // Configure AccountCode as owned type
        builder.OwnsOne(x => x.Code, code =>
        {
            code.Property(c => c.Code)
                .HasColumnName("AccountCode")
                .IsRequired()
                .HasMaxLength(5);
        });

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.SubType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Description)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.Property(x => x.UpdatedBy)
            .IsRequired(false);

        builder.Property(x => x.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        // Indices
        builder.HasIndex(x => x.ChartOfAccountsId);
        builder.HasIndex(x => new { x.ChartOfAccountsId, x.IsArchived });
    }
}

/// <summary>
/// EF Core configuration for JournalEntry aggregate root.
/// </summary>
public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ReferenceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.EntryDate)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        // Configure Money value objects
        builder.OwnsOne(x => x.TotalDebits, debits =>
        {
            debits.Property(m => m.Amount)
                .HasColumnName("TotalDebits")
                .HasPrecision(18, 2);
            debits.OwnsOne(m => m.Currency, currency =>
            {
                currency.Property(c => c.Code)
                    .HasColumnName("TotalDebits_CurrencyCode")
                    .HasConversion<string>();
            });
        });

        builder.OwnsOne(x => x.TotalCredits, credits =>
        {
            credits.Property(m => m.Amount)
                .HasColumnName("TotalCredits")
                .HasPrecision(18, 2);
            credits.OwnsOne(m => m.Currency, currency =>
            {
                currency.Property(c => c.Code)
                    .HasColumnName("TotalCredits_CurrencyCode")
                    .HasConversion<string>();
            });
        });

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.Property(x => x.UpdatedBy)
            .IsRequired(false);

        builder.Property(x => x.PostedAt)
            .IsRequired(false);

        builder.Property(x => x.PostedBy)
            .IsRequired(false);

        // Navigation to lines
        builder.HasMany<JournalLine>()
            .WithOne(x => x.JournalEntry)
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indices
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => x.ReferenceNumber).IsUnique(false);
        builder.HasIndex(x => x.EntryDate);
    }
}

/// <summary>
/// EF Core configuration for JournalLine entity.
/// </summary>
public class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> builder)
    {
        builder.ToTable("JournalLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.JournalEntryId)
            .IsRequired();

        // Configure AccountCode as owned type
        builder.OwnsOne(x => x.AccountCode, code =>
        {
            code.Property(c => c.Code)
                .HasColumnName("AccountCode")
                .IsRequired()
                .HasMaxLength(5);
        });

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        // Configure Money value object
        builder.OwnsOne(x => x.Amount, amount =>
        {
            amount.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2);
            amount.OwnsOne(m => m.Currency, currency =>
            {
                currency.Property(c => c.Code)
                    .HasColumnName("CurrencyCode")
                    .HasConversion<string>();
            });
        });

        builder.Property(x => x.IsDebit)
            .IsRequired();

        builder.Property(x => x.LineNumber)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        // Indices
        builder.HasIndex(x => x.JournalEntryId);
        builder.HasIndex(x => new { x.JournalEntryId, x.LineNumber });
    }
}

/// <summary>
/// EF Core configuration for AccountBalance entity.
/// </summary>
public class AccountBalanceConfiguration : IEntityTypeConfiguration<AccountBalance>
{
    public void Configure(EntityTypeBuilder<AccountBalance> builder)
    {
        builder.ToTable("AccountBalances");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        // Configure AccountCode as owned type
        builder.OwnsOne(x => x.AccountCode, code =>
        {
            code.Property(c => c.Code)
                .HasColumnName("AccountCode")
                .IsRequired()
                .HasMaxLength(5);
        });

        builder.Property(x => x.BalanceDate)
            .IsRequired();

        // Configure Money value objects
        builder.OwnsOne(x => x.CurrentBalance, balance =>
        {
            balance.Property(m => m.Amount)
                .HasColumnName("CurrentBalance")
                .HasPrecision(18, 2);
            balance.OwnsOne(m => m.Currency, currency =>
            {
                currency.Property(c => c.Code)
                    .HasColumnName("CurrentBalance_CurrencyCode")
                    .HasConversion<string>();
            });
        });

        builder.OwnsOne(x => x.DebitBalance, balance =>
        {
            balance.Property(m => m.Amount)
                .HasColumnName("DebitBalance")
                .HasPrecision(18, 2);
            balance.OwnsOne(m => m.Currency, currency =>
            {
                currency.Property(c => c.Code)
                    .HasColumnName("DebitBalance_CurrencyCode")
                    .HasConversion<string>();
            });
        });

        builder.OwnsOne(x => x.CreditBalance, balance =>
        {
            balance.Property(m => m.Amount)
                .HasColumnName("CreditBalance")
                .HasPrecision(18, 2);
            balance.OwnsOne(m => m.Currency, currency =>
            {
                currency.Property(c => c.Code)
                    .HasColumnName("CreditBalance_CurrencyCode")
                    .HasConversion<string>();
            });
        });

        builder.Property(x => x.LastUpdated)
            .IsRequired();

        builder.Property(x => x.LastUpdatedBy)
            .IsRequired();

        builder.Property(x => x.FiscalYear)
            .IsRequired();

        builder.Property(x => x.FiscalPeriod)
            .IsRequired();

        // Indices
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.FiscalYear, x.FiscalPeriod });
        builder.HasIndex(x => x.BalanceDate);
    }
}
