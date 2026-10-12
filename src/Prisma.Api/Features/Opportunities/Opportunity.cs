using Prisma.Api.Features.Diagnoses;
using Prisma.Api.Features.Phases;

namespace Prisma.Api.Features.Opportunities;

public sealed class Opportunity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DiagnosisId { get; set; }

    public Guid? PhaseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? SolutionDescription { get; set; }

    public string Type { get; set; } = string.Empty;

    public string ExpectedBenefit { get; set; } = string.Empty;

    public int Impact { get; set; }

    public int Effort { get; set; }

    public int Urgency { get; set; }

    public string? Dependencies { get; set; }

    public string? AiDataNeeds { get; set; }

    public bool? NeedsHumanReview { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public Diagnosis Diagnosis { get; set; } = null!;

    public Phase? Phase { get; set; }

    public ICollection<OpportunityBottleneck> BottleneckLinks { get; set; } = new List<OpportunityBottleneck>();
}
