using Prisma.Api.Features.Diagnoses;
using Prisma.Api.Features.Opportunities;
using Prisma.Api.Features.SectorAssessments;

namespace Prisma.Api.Features.Bottlenecks;

public sealed class Bottleneck
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DiagnosisId { get; set; }

    public Guid SectorAssessmentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Category { get; set; } = string.Empty;

    public int Impact { get; set; }

    public string? Frequency { get; set; }

    public string? Evidence { get; set; }

    public string? ProbableCause { get; set; }

    public decimal? EstimatedMonthlyHours { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public Diagnosis Diagnosis { get; set; } = null!;

    public SectorAssessment SectorAssessment { get; set; } = null!;

    public ICollection<OpportunityBottleneck> OpportunityLinks { get; set; } = new List<OpportunityBottleneck>();
}
