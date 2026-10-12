using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prisma.Api.Features.Diagnoses;
using Prisma.Api.Features.SectorAssessments;

namespace Prisma.Api.Features.Bottlenecks;

public sealed class BottleneckEntityConfiguration : IEntityTypeConfiguration<Bottleneck>
{
    public void Configure(EntityTypeBuilder<Bottleneck> builder)
    {
        builder.ToTable("bottlenecks", table =>
        {
            table.HasCheckConstraint("ck_bottlenecks_impact_range", "\"Impact\" BETWEEN 1 AND 5");
            table.HasCheckConstraint(
                "ck_bottlenecks_estimated_monthly_hours_non_negative",
                "\"EstimatedMonthlyHours\" IS NULL OR \"EstimatedMonthlyHours\" >= 0");
        });
        builder.HasKey(bottleneck => bottleneck.Id).HasName("pk_bottlenecks");
        builder.HasAlternateKey(bottleneck => new { bottleneck.Id, bottleneck.DiagnosisId })
            .HasName("ak_bottlenecks_id_diagnosis_id");

        builder.Property(bottleneck => bottleneck.Id).ValueGeneratedNever();
        builder.Property(bottleneck => bottleneck.DiagnosisId).IsRequired();
        builder.Property(bottleneck => bottleneck.SectorAssessmentId).IsRequired();
        builder.Property(bottleneck => bottleneck.Title).IsRequired().HasColumnType("text");
        builder.Property(bottleneck => bottleneck.Description).HasColumnType("text");
        builder.Property(bottleneck => bottleneck.Category).IsRequired().HasColumnType("text");
        builder.Property(bottleneck => bottleneck.Impact).IsRequired();
        builder.Property(bottleneck => bottleneck.Frequency).HasColumnType("text");
        builder.Property(bottleneck => bottleneck.Evidence).HasColumnType("text");
        builder.Property(bottleneck => bottleneck.ProbableCause).HasColumnType("text");
        builder.Property(bottleneck => bottleneck.EstimatedMonthlyHours).HasColumnType("numeric");
        builder.Property(bottleneck => bottleneck.Notes).HasColumnType("text");
        builder.Property(bottleneck => bottleneck.CreatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(bottleneck => bottleneck.UpdatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");

        builder.HasOne(bottleneck => bottleneck.Diagnosis)
            .WithMany(diagnosis => diagnosis.Bottlenecks)
            .HasForeignKey(bottleneck => bottleneck.DiagnosisId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_bottlenecks_diagnoses_diagnosis_id");

        builder.HasOne(bottleneck => bottleneck.SectorAssessment)
            .WithMany(assessment => assessment.Bottlenecks)
            .HasForeignKey(bottleneck => new { bottleneck.SectorAssessmentId, bottleneck.DiagnosisId })
            .HasPrincipalKey(assessment => new { assessment.Id, assessment.DiagnosisId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_bottlenecks_sector_assessments_assessment_diagnosis_id");
    }
}
