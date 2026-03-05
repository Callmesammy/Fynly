namespace AiCFO.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiCFO.Domain.Entities;

/// <summary>
/// EF Core configuration for PredictiveThreshold entity.
/// </summary>
public class PredictiveThresholdConfiguration : IEntityTypeConfiguration<PredictiveThreshold>
{
    public void Configure(EntityTypeBuilder<PredictiveThreshold> builder)
    {
        builder.ToTable("PredictiveThresholds");

        builder.HasKey(t => t.PredictiveThresholdId);

        builder.Property(t => t.PredictiveThresholdId)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(t => t.TenantId)
            .IsRequired();

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        // Owned type configuration for PredictiveThresholdValue
        builder.OwnsOne(t => t.ThresholdValue, ov =>
        {
            ov.Property(v => v.Type).HasColumnName("ThresholdType").IsRequired();
            ov.Property(v => v.Operator).HasColumnName("ThresholdOperator").IsRequired();
            ov.Property(v => v.Value).HasPrecision(18, 6).IsRequired();
            ov.Property(v => v.MaxValue).HasPrecision(18, 6);
            ov.Property(v => v.Severity).HasColumnName("AlertSeverity").IsRequired();
        });

        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.LastEvaluatedAt);
        builder.Property(t => t.AlertCountSinceLastReset).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt);
        builder.Property(t => t.CreatedBy);
        builder.Property(t => t.UpdatedBy);
        builder.Property(t => t.IsDeleted).IsRequired();

        // Indices for performance
        builder.HasIndex(t => t.TenantId).HasDatabaseName("IX_PredictiveThresholds_TenantId");
        builder.HasIndex(t => new { t.TenantId, t.IsActive }).HasDatabaseName("IX_PredictiveThresholds_TenantId_IsActive");
        builder.HasIndex(t => t.IsDeleted).HasDatabaseName("IX_PredictiveThresholds_IsDeleted");
    }
}

/// <summary>
/// EF Core configuration for PredictiveAlert entity.
/// </summary>
public class PredictiveAlertConfiguration : IEntityTypeConfiguration<PredictiveAlert>
{
    public void Configure(EntityTypeBuilder<PredictiveAlert> builder)
    {
        builder.ToTable("PredictiveAlerts");

        builder.HasKey(a => a.PredictiveAlertId);

        builder.Property(a => a.PredictiveAlertId)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.ThresholdId)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.TriggeredValue)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(a => a.TriggeredAt)
            .IsRequired();

        builder.Property(a => a.AcknowledgedAt);
        builder.Property(a => a.AcknowledgmentNotes).HasMaxLength(500);
        builder.Property(a => a.ResolvedAt);
        builder.Property(a => a.ResolutionNotes).HasMaxLength(500);

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt);
        builder.Property(a => a.CreatedBy);
        builder.Property(a => a.UpdatedBy);
        builder.Property(a => a.IsDeleted).IsRequired();

        // Indices for performance
        builder.HasIndex(a => a.TenantId).HasDatabaseName("IX_PredictiveAlerts_TenantId");
        builder.HasIndex(a => new { a.TenantId, a.Status }).HasDatabaseName("IX_PredictiveAlerts_TenantId_Status");
        builder.HasIndex(a => a.TriggeredAt).HasDatabaseName("IX_PredictiveAlerts_TriggeredAt");
        builder.HasIndex(a => a.IsDeleted).HasDatabaseName("IX_PredictiveAlerts_IsDeleted");
    }
}
