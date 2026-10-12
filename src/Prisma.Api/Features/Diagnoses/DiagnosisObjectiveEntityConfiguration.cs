using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Prisma.Api.Features.Diagnoses;

public sealed class DiagnosisObjectiveEntityConfiguration : IEntityTypeConfiguration<DiagnosisObjective>
{
    public void Configure(EntityTypeBuilder<DiagnosisObjective> builder)
    {
        builder.ToTable("diagnosis_objectives");
        builder.HasKey(objective => new { objective.DiagnosisId, objective.Code })
            .HasName("pk_diagnosis_objectives");

        builder.Property(objective => objective.DiagnosisId).IsRequired();
        builder.Property(objective => objective.Code).IsRequired().HasColumnType("text");
        builder.Property(objective => objective.IsPrimary).IsRequired();
        builder.Property(objective => objective.CustomDescription).HasColumnType("text");

        builder.HasIndex(objective => objective.DiagnosisId)
            .IsUnique()
            .HasFilter("\"IsPrimary\" = TRUE")
            .HasDatabaseName("ux_diagnosis_objectives_one_primary_per_diagnosis");

        builder.HasOne(objective => objective.Diagnosis)
            .WithMany(diagnosis => diagnosis.Objectives)
            .HasForeignKey(objective => objective.DiagnosisId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_diagnosis_objectives_diagnoses_diagnosis_id");
    }
}
