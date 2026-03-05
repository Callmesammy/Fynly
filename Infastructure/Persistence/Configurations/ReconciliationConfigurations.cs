namespace AiCFO.Infrastructure.Persistence.Configurations;

using AiCFO.Domain.Entities;
using AiCFO.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core configuration for ReconciliationMatch entity.
/// </summary>
public class ReconciliationMatchConfiguration : IEntityTypeConfiguration<ReconciliationMatch>
{
    public void Configure(EntityTypeBuilder<ReconciliationMatch> builder)
    {
        builder.ToTable("ReconciliationMatches");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("ReconciliationMatchId")
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired()
            .HasColumnName("TenantId");

        builder.Property(x => x.BankTransactionId)
            .IsRequired()
            .HasColumnName("BankTransactionId");

        builder.Property(x => x.JournalEntryId)
            .IsRequired()
            .HasColumnName("JournalEntryId");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasColumnName("ReconciliationStatus")
            .HasConversion<int>();

        // Owned MatchScore value object
        builder.OwnsOne(x => x.MatchScore, scoreBuilder =>
        {
            scoreBuilder.Property(s => s.ConfidencePercentage).HasColumnName("MatchConfidencePercentage");
            scoreBuilder.Property(s => s.ConfidenceLevel).HasColumnName("MatchConfidenceLevel").HasConversion<int>();
            scoreBuilder.Property(s => s.MatchType).HasColumnName("MatchType").HasConversion<int>();
            scoreBuilder.Property(s => s.MatchReason).HasColumnName("MatchReason").HasMaxLength(500);
        });

        // Owned VarianceAmount value object
        builder.OwnsOne(x => x.VarianceAmount, varianceBuilder =>
        {
            varianceBuilder.OwnsOne(v => v.Amount, amountBuilder =>
            {
                amountBuilder.Property(a => a.Amount).HasColumnName("VarianceAmount_Value").HasPrecision(18, 2);
                amountBuilder.Property(a => a.Currency.Code).HasColumnName("VarianceAmount_CurrencyCode").HasConversion<string>().HasMaxLength(3);
            });
            varianceBuilder.Property(v => v.Percentage).HasColumnName("VariancePercentage");
            varianceBuilder.Property(v => v.IsSignificant).HasColumnName("IsSignificantVariance");
        });

        // Owned TimelineVariance value object
        builder.OwnsOne(x => x.TimelineVariance, timelineBuilder =>
        {
            timelineBuilder.Property(t => t.DaysDifference).HasColumnName("DaysDifference");
            timelineBuilder.Property(t => t.IsSignificant).HasColumnName("IsSignificantTimeDifference");
        });

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.MatchedAt)
            .IsRequired();

        builder.Property(x => x.MatchedBy)
            .IsRequired();

        builder.Property(x => x.ConfirmedAt);
        builder.Property(x => x.ConfirmedBy);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.UpdatedBy);

        // Relationship to BankTransaction
        builder.HasOne<BankTransaction>()
            .WithMany()
            .HasForeignKey(x => x.BankTransactionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship to JournalEntry
        builder.HasOne<JournalEntry>()
            .WithMany()
            .HasForeignKey(x => x.JournalEntryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Audit logs
        builder.HasMany(x => x.AuditLogs)
            .WithOne()
            .HasForeignKey(x => x.ReconciliationMatchId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Indices for common queries
        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_ReconciliationMatches_TenantId_Status");

        builder.HasIndex(x => new { x.TenantId, x.BankTransactionId })
            .HasDatabaseName("IX_ReconciliationMatches_TenantId_BankTransactionId");

        builder.HasIndex(x => new { x.TenantId, x.JournalEntryId })
            .HasDatabaseName("IX_ReconciliationMatches_TenantId_JournalEntryId");

        builder.HasIndex(x => new { x.TenantId, x.MatchedAt })
            .HasDatabaseName("IX_ReconciliationMatches_TenantId_MatchedAt");
    }
}

/// <summary>
/// EF Core configuration for ReconciliationAuditLog entity.
/// </summary>
public class ReconciliationAuditLogConfiguration : IEntityTypeConfiguration<ReconciliationAuditLog>
{
    public void Configure(EntityTypeBuilder<ReconciliationAuditLog> builder)
    {
        builder.ToTable("ReconciliationAuditLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("AuditLogId")
            .ValueGeneratedNever();

        builder.Property(x => x.ReconciliationMatchId)
            .IsRequired();

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.PreviousValue)
            .HasMaxLength(500);

        builder.Property(x => x.NewValue)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.ReconciliationMatchId)
            .HasDatabaseName("IX_ReconciliationAuditLogs_ReconciliationMatchId");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_ReconciliationAuditLogs_CreatedAt");
    }
}

/// <summary>
/// EF Core configuration for ReconciliationSession entity.
/// </summary>
public class ReconciliationSessionConfiguration : IEntityTypeConfiguration<ReconciliationSession>
{
    public void Configure(EntityTypeBuilder<ReconciliationSession> builder)
    {
        builder.ToTable("ReconciliationSessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("ReconciliationSessionId")
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.SessionDate)
            .IsRequired();

        builder.Property(x => x.SessionName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.TotalTransactionsProcessed)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.MatchesFound)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.MatchesConfirmed)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.MatchesRejected)
            .IsRequired()
            .HasDefaultValue(0);

        // Owned TotalMatchedAmount
        builder.OwnsOne(x => x.TotalMatchedAmount, amountBuilder =>
        {
            amountBuilder.Property(a => a.Amount).HasColumnName("TotalMatchedAmount_Value").HasPrecision(18, 2);
            amountBuilder.Property(a => a.Currency.Code).HasColumnName("TotalMatchedAmount_CurrencyCode").HasConversion<string>().HasMaxLength(3);
        });

        // Owned TotalUnmatchedAmount
        builder.OwnsOne(x => x.TotalUnmatchedAmount, amountBuilder =>
        {
            amountBuilder.Property(a => a.Amount).HasColumnName("TotalUnmatchedAmount_Value").HasPrecision(18, 2);
            amountBuilder.Property(a => a.Currency.Code).HasColumnName("TotalUnmatchedAmount_CurrencyCode").HasConversion<string>().HasMaxLength(3);
        });

        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.UpdatedBy);

        // Store MatchIds as JSON array
        builder.Property(x => x.MatchIds)
            .HasColumnName("MatchIds")
            .HasColumnType("jsonb");

        builder.HasIndex(x => new { x.TenantId, x.SessionDate })
            .HasDatabaseName("IX_ReconciliationSessions_TenantId_SessionDate");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_ReconciliationSessions_CreatedAt");
    }
}

/// <summary>
/// EF Core configuration for UnmatchedBankTransaction entity.
/// </summary>
public class UnmatchedBankTransactionConfiguration : IEntityTypeConfiguration<UnmatchedBankTransaction>
{
    public void Configure(EntityTypeBuilder<UnmatchedBankTransaction> builder)
    {
        builder.ToTable("UnmatchedBankTransactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("UnmatchedBankTransactionId")
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.BankTransactionId)
            .IsRequired();

        builder.Property(x => x.TransactionReference)
            .IsRequired()
            .HasMaxLength(100);

        builder.OwnsOne(x => x.Amount, amountBuilder =>
        {
            amountBuilder.Property(a => a.Amount).HasColumnName("Amount_Value").HasPrecision(18, 2);
            amountBuilder.Property(a => a.Currency.Code).HasColumnName("Amount_CurrencyCode").HasConversion<string>().HasMaxLength(3);
        });

        builder.Property(x => x.TransactionDate)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.DaysUnmatched)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.DaysUnmatched })
            .HasDatabaseName("IX_UnmatchedBankTransactions_TenantId_DaysUnmatched");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_UnmatchedBankTransactions_CreatedAt");
    }
}

/// <summary>
/// EF Core configuration for UnmatchedJournalEntry entity.
/// </summary>
public class UnmatchedJournalEntryConfiguration : IEntityTypeConfiguration<UnmatchedJournalEntry>
{
    public void Configure(EntityTypeBuilder<UnmatchedJournalEntry> builder)
    {
        builder.ToTable("UnmatchedJournalEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("UnmatchedJournalEntryId")
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.JournalEntryId)
            .IsRequired();

        builder.Property(x => x.ReferenceNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.OwnsOne(x => x.Amount, amountBuilder =>
        {
            amountBuilder.Property(a => a.Amount).HasColumnName("Amount_Value").HasPrecision(18, 2);
            amountBuilder.Property(a => a.Currency.Code).HasColumnName("Amount_CurrencyCode").HasConversion<string>().HasMaxLength(3);
        });

        builder.Property(x => x.EntryDate)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.DaysUnmatched)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.DaysUnmatched })
            .HasDatabaseName("IX_UnmatchedJournalEntries_TenantId_DaysUnmatched");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_UnmatchedJournalEntries_CreatedAt");
    }
}
