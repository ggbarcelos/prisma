using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prisma.Api.Features.Bottlenecks;

namespace Prisma.Api.Features.Opportunities;

public sealed class OpportunityBottleneckEntityConfiguration : IEntityTypeConfiguration<OpportunityBottleneck>
{
    public void Configure(EntityTypeBuilder<OpportunityBottleneck> builder)
    {
        builder.ToTable("opportunity_bottlenecks");
        builder.HasKey(link => new { link.OpportunityId, link.BottleneckId })
            .HasName("pk_opportunity_bottlenecks");

        builder.Property(link => link.DiagnosisId).IsRequired();
        builder.Property(link => link.OpportunityId).IsRequired();
        builder.Property(link => link.BottleneckId).IsRequired();

        builder.HasOne(link => link.Opportunity)
            .WithMany(opportunity => opportunity.BottleneckLinks)
            .HasForeignKey(link => new { link.OpportunityId, link.DiagnosisId })
            .HasPrincipalKey(opportunity => new { opportunity.Id, opportunity.DiagnosisId })
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_opportunity_bottlenecks_opportunities_opportunity_diagnosis_id");

        builder.HasOne(link => link.Bottleneck)
            .WithMany(bottleneck => bottleneck.OpportunityLinks)
            .HasForeignKey(link => new { link.BottleneckId, link.DiagnosisId })
            .HasPrincipalKey(bottleneck => new { bottleneck.Id, bottleneck.DiagnosisId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_opportunity_bottlenecks_bottlenecks_bottleneck_diagnosis_id");
    }
}
