namespace AiCFO.Infrastructure.Persistence.Configurations;

using AiCFO.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core entity type configuration for HealthReport.
/// </summary>
public class HealthReportConfiguration : IEntityTypeConfiguration<HealthReport>
{
    /// <summary>
    /// Configure the HealthReport entity.
    /// </summary>
    public void Configure(EntityTypeBuilder<HealthReport> builder)
    {
        // Table mapping
        builder.ToTable("HealthReports");

        // Primary key
        builder.HasKey(r => r.Id);

        // Property configurations
        builder.Property(r => r.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(r => r.ReportId)
            .HasColumnName("ReportId")
            .IsRequired();

        builder.Property(r => r.TenantId)
            .HasColumnName("TenantId")
            .IsRequired();

        builder.Property(r => r.ReportType)
            .HasColumnName("ReportType")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.OverallScore)
            .HasColumnName("OverallScore")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(r => r.Rating)
            .HasColumnName("Rating")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.LiquidityScore)
            .HasColumnName("LiquidityScore")
            .HasPrecision(5, 2);

        builder.Property(r => r.ProfitabilityScore)
            .HasColumnName("ProfitabilityScore")
            .HasPrecision(5, 2);

        builder.Property(r => r.SolvencyScore)
            .HasColumnName("SolvencyScore")
            .HasPrecision(5, 2);

        builder.Property(r => r.EfficiencyScore)
            .HasColumnName("EfficiencyScore")
            .HasPrecision(5, 2);

        builder.Property(r => r.GrowthScore)
            .HasColumnName("GrowthScore")
            .HasPrecision(5, 2);

        builder.Property(r => r.CriticalRecommendationsCount)
            .HasColumnName("CriticalRecommendationsCount")
            .IsRequired();

        builder.Property(r => r.HighRecommendationsCount)
            .HasColumnName("HighRecommendationsCount")
            .IsRequired();

        builder.Property(r => r.AnomaliesDetected)
            .HasColumnName("AnomaliesDetected")
            .IsRequired();

        builder.Property(r => r.GeneratedAt)
            .HasColumnName("GeneratedAt")
            .IsRequired();

        builder.Property(r => r.ScheduledFor)
            .HasColumnName("ScheduledFor");

        builder.Property(r => r.SentAt)
            .HasColumnName("SentAt");

        builder.Property(r => r.ExpiresAt)
            .HasColumnName("ExpiresAt");

        builder.Property(r => r.Frequency)
            .HasColumnName("Frequency")
            .HasConversion<int?>();

        builder.Property(r => r.Recipients)
            .HasColumnName("Recipients")
            .HasMaxLength(1000);

        builder.Property(r => r.Summary)
            .HasColumnName("Summary")
            .HasMaxLength(2000);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(r => r.CreatedBy)
            .HasColumnName("CreatedBy")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("UpdatedAt");

        builder.Property(r => r.UpdatedBy)
            .HasColumnName("UpdatedBy");

        builder.Property(r => r.IsDeleted)
            .HasColumnName("IsDeleted")
            .IsRequired();

        // Indices for performance
        builder.HasIndex(r => new { r.TenantId, r.GeneratedAt })
            .HasDatabaseName("IX_HealthReports_TenantId_GeneratedAt");

        builder.HasIndex(r => new { r.TenantId, r.Status })
            .HasDatabaseName("IX_HealthReports_TenantId_Status");

        builder.HasIndex(r => new { r.TenantId, r.ScheduledFor })
            .HasDatabaseName("IX_HealthReports_TenantId_ScheduledFor")
            .HasFilter("\"ScheduledFor\" IS NOT NULL");
    }
}
