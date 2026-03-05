namespace AiCFO.Infrastructure.Persistence.Configurations;

using AiCFO.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core configuration for AlertNotification entity.
/// Handles table mapping, relationships, constraints, and owned types.
/// </summary>
public class AlertNotificationConfiguration : IEntityTypeConfiguration<AlertNotification>
{
    public void Configure(EntityTypeBuilder<AlertNotification> builder)
    {
        builder.ToTable("Alerts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("AlertNotificationId")
            .ValueGeneratedNever();

        builder.Property(x => x.AlertId)
            .IsRequired()
            .HasColumnName("AlertId");

        builder.Property(x => x.TenantId)
            .IsRequired()
            .HasColumnName("TenantId");

        builder.Property(x => x.AnomalyReference)
            .IsRequired()
            .HasColumnName("AnomalyReference")
            .HasMaxLength(256);

        builder.Property(x => x.Severity)
            .IsRequired()
            .HasColumnName("AnomalySeverity")
            .HasConversion<int>();

        builder.Property(x => x.ConfidenceScore)
            .IsRequired()
            .HasColumnName("ConfidenceScore")
            .HasPrecision(5, 2); // 0-100 with 2 decimal places

        builder.Property(x => x.Message)
            .IsRequired()
            .HasColumnName("AlertMessage")
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasColumnName("AlertStatus")
            .HasConversion<int>();

        builder.Property(x => x.AcknowledgedBy)
            .HasMaxLength(256);

        builder.Property(x => x.AcknowledgedAt);

        builder.Property(x => x.ResolutionNotes)
            .HasMaxLength(2000);

        builder.Property(x => x.ResolvedAt);

        builder.Property(x => x.AutoDismissHours)
            .IsRequired()
            .HasColumnName("AutoDismissHours");

        // Aggregated anomalies list stored as JSON
        builder.Property(x => x.AggregatedAnomalies)
            .HasColumnName("AggregatedAnomalies")
            .HasConversion(
                v => string.Join(";", v),
                v => v.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(5000);

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.UpdatedBy);

        // Indices for performance
        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_Alerts_TenantId_Status");

        builder.HasIndex(x => new { x.TenantId, x.Severity })
            .HasDatabaseName("IX_Alerts_TenantId_Severity");

        builder.HasIndex(x => new { x.TenantId, x.CreatedAt })
            .HasDatabaseName("IX_Alerts_TenantId_CreatedAt")
            .IsDescending(false, true); // Descending on CreatedAt for recent alerts

        builder.HasIndex(x => x.AlertId)
            .HasDatabaseName("IX_Alerts_AlertId")
            .IsUnique();

        // Relationship constraints
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
    }
}
