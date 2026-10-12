using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Prisma.Api.Features.Phases;

public sealed class PhaseEntityConfiguration : IEntityTypeConfiguration<Phase>
{
    public void Configure(EntityTypeBuilder<Phase> builder)
    {
        builder.ToTable("phases", table =>
            table.HasCheckConstraint("ck_phases_sort_order_nonnegative", "\"SortOrder\" >= 0"));
        builder.HasKey(phase => phase.Id).HasName("pk_phases");
        builder.HasAlternateKey(phase => new { phase.Id, phase.DiagnosisId })
            .HasName("ak_phases_id_diagnosis_id");
        builder.HasIndex(phase => new { phase.DiagnosisId, phase.SortOrder })
            .IsUnique()
            .HasDatabaseName("ux_phases_diagnosis_id_sort_order");

        builder.Property(phase => phase.Id).ValueGeneratedNever();
        builder.Property(phase => phase.DiagnosisId).IsRequired();
        builder.Property(phase => phase.Name).IsRequired().HasColumnType("text");
        builder.Property(phase => phase.Description).HasColumnType("text");
        builder.Property(phase => phase.SortOrder).IsRequired();
        builder.Property(phase => phase.CreatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(phase => phase.UpdatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");

        builder.HasOne(phase => phase.Diagnosis)
            .WithMany(diagnosis => diagnosis.Phases)
            .HasForeignKey(phase => phase.DiagnosisId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_phases_diagnoses_diagnosis_id");
    }
}
