using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prisma.Api.Features.Diagnoses;

namespace Prisma.Api.Features.Opportunities;

public sealed class OpportunityEntityConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> builder)
    {
        builder.ToTable("opportunities", table =>
        {
            table.HasCheckConstraint("ck_opportunities_impact_range", "\"Impact\" BETWEEN 1 AND 5");
            table.HasCheckConstraint("ck_opportunities_effort_range", "\"Effort\" BETWEEN 1 AND 5");
            table.HasCheckConstraint("ck_opportunities_urgency_range", "\"Urgency\" BETWEEN 1 AND 5");
        });
        builder.HasKey(opportunity => opportunity.Id).HasName("pk_opportunities");
        builder.HasAlternateKey(opportunity => new { opportunity.Id, opportunity.DiagnosisId })
            .HasName("ak_opportunities_id_diagnosis_id");

        builder.Property(opportunity => opportunity.Id).ValueGeneratedNever();
        builder.Property(opportunity => opportunity.DiagnosisId).IsRequired();
        builder.Property(opportunity => opportunity.PhaseId);
        builder.Property(opportunity => opportunity.Title).IsRequired().HasColumnType("text");
        builder.Property(opportunity => opportunity.SolutionDescription).HasColumnType("text");
        builder.Property(opportunity => opportunity.Type).IsRequired().HasColumnType("text");
        builder.Property(opportunity => opportunity.ExpectedBenefit).IsRequired().HasColumnType("text");
        builder.Property(opportunity => opportunity.Impact).IsRequired();
        builder.Property(opportunity => opportunity.Effort).IsRequired();
        builder.Property(opportunity => opportunity.Urgency).IsRequired();
        builder.Property(opportunity => opportunity.Dependencies).HasColumnType("text");
        builder.Property(opportunity => opportunity.AiDataNeeds).HasColumnType("text");
        builder.Property(opportunity => opportunity.NeedsHumanReview);
        builder.Property(opportunity => opportunity.CreatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(opportunity => opportunity.UpdatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");

        builder.HasOne(opportunity => opportunity.Diagnosis)
            .WithMany(diagnosis => diagnosis.Opportunities)
            .HasForeignKey(opportunity => opportunity.DiagnosisId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_opportunities_diagnoses_diagnosis_id");

        builder.HasOne(opportunity => opportunity.Phase)
            .WithMany(phase => phase.Opportunities)
            .HasForeignKey(opportunity => new { opportunity.PhaseId, opportunity.DiagnosisId })
            .HasPrincipalKey(phase => new { phase.Id, phase.DiagnosisId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_opportunities_phases_phase_id_diagnosis_id");
    }
}
