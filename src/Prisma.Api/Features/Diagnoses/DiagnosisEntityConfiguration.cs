using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prisma.Api.Features.Clients;

namespace Prisma.Api.Features.Diagnoses;

public sealed class DiagnosisEntityConfiguration : IEntityTypeConfiguration<Diagnosis>
{
    public void Configure(EntityTypeBuilder<Diagnosis> builder)
    {
        builder.ToTable("diagnoses");
        builder.HasKey(diagnosis => diagnosis.Id).HasName("pk_diagnoses");
        builder.HasAlternateKey(diagnosis => new { diagnosis.Id, diagnosis.ClientId })
            .HasName("ak_diagnoses_id_client_id");

        builder.Property(diagnosis => diagnosis.Id).ValueGeneratedNever();
        builder.Property(diagnosis => diagnosis.ClientId).IsRequired();
        builder.Property(diagnosis => diagnosis.Title).IsRequired().HasColumnType("text");
        builder.Property(diagnosis => diagnosis.AssessmentDate).IsRequired().HasColumnType("date");
        builder.Property(diagnosis => diagnosis.Context).HasColumnType("text");
        builder.Property(diagnosis => diagnosis.Status)
            .HasConversion<string>()
            .HasColumnType("text")
            .HasDefaultValue(DiagnosisStatus.Draft)
            .IsRequired();
        builder.Property(diagnosis => diagnosis.ExecutiveSummary).HasColumnType("text");
        builder.Property(diagnosis => diagnosis.GeneralRecommendations).HasColumnType("text");
        builder.Property(diagnosis => diagnosis.NextSteps).HasColumnType("text");
        builder.Property(diagnosis => diagnosis.CreatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(diagnosis => diagnosis.UpdatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");

        builder.HasOne(diagnosis => diagnosis.Client)
            .WithMany(client => client.Diagnoses)
            .HasForeignKey(diagnosis => diagnosis.ClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_diagnoses_clients_client_id");
    }
}
