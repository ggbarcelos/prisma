using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prisma.Api.Features.Clients;
using Prisma.Api.Features.Diagnoses;

namespace Prisma.Api.Features.SectorAssessments;

public sealed class SectorAssessmentEntityConfiguration : IEntityTypeConfiguration<SectorAssessment>
{
    public void Configure(EntityTypeBuilder<SectorAssessment> builder)
    {
        builder.ToTable("sector_assessments");
        builder.HasKey(assessment => assessment.Id).HasName("pk_sector_assessments");
        builder.HasAlternateKey(assessment => new { assessment.Id, assessment.DiagnosisId })
            .HasName("ak_sector_assessments_id_diagnosis_id");

        builder.Property(assessment => assessment.Id).ValueGeneratedNever();
        builder.Property(assessment => assessment.DiagnosisId).IsRequired();
        builder.Property(assessment => assessment.ClientId).IsRequired();
        builder.Property(assessment => assessment.ClientSectorId).IsRequired();
        builder.Property(assessment => assessment.SectorNameSnapshot).IsRequired().HasColumnType("text");
        builder.Property(assessment => assessment.Answer01).HasColumnType("text");
        builder.Property(assessment => assessment.Answer02).HasColumnType("text");
        builder.Property(assessment => assessment.Answer03).HasColumnType("text");
        builder.Property(assessment => assessment.Answer04).HasColumnType("text");
        builder.Property(assessment => assessment.Answer05).HasColumnType("text");
        builder.Property(assessment => assessment.Answer06).HasColumnType("text");
        builder.Property(assessment => assessment.Answer07).HasColumnType("text");
        builder.Property(assessment => assessment.Answer08).HasColumnType("text");
        builder.Property(assessment => assessment.Answer09).HasColumnType("text");
        builder.Property(assessment => assessment.Answer10).HasColumnType("text");
        builder.Property(assessment => assessment.CreatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(assessment => assessment.UpdatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");

        builder.HasIndex(assessment => new { assessment.DiagnosisId, assessment.ClientSectorId })
            .IsUnique()
            .HasDatabaseName("ux_sector_assessments_diagnosis_client_sector");

        builder.HasOne(assessment => assessment.Diagnosis)
            .WithMany(diagnosis => diagnosis.Assessments)
            .HasForeignKey(assessment => new { assessment.DiagnosisId, assessment.ClientId })
            .HasPrincipalKey(diagnosis => new { diagnosis.Id, diagnosis.ClientId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sector_assessments_diagnoses_diagnosis_client_id");

        builder.HasOne(assessment => assessment.ClientSector)
            .WithMany()
            .HasForeignKey(assessment => new { assessment.ClientSectorId, assessment.ClientId })
            .HasPrincipalKey(sector => new { sector.Id, sector.ClientId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sector_assessments_client_sectors_sector_client_id");
    }
}
