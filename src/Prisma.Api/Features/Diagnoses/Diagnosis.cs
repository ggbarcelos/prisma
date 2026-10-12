using Prisma.Api.Features.Clients;
using Prisma.Api.Features.Bottlenecks;
using Prisma.Api.Features.Opportunities;
using Prisma.Api.Features.Phases;
using Prisma.Api.Features.SectorAssessments;

namespace Prisma.Api.Features.Diagnoses;

public sealed class Diagnosis
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClientId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateOnly AssessmentDate { get; set; }

    public string? Context { get; set; }

    public DiagnosisStatus Status { get; set; } = DiagnosisStatus.Draft;

    public string? ExecutiveSummary { get; set; }

    public string? GeneralRecommendations { get; set; }

    public string? NextSteps { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public global::Prisma.Api.Features.Clients.Client Client { get; set; } = null!;

    public ICollection<DiagnosisObjective> Objectives { get; set; } = new List<DiagnosisObjective>();

    public ICollection<SectorAssessment> Assessments { get; set; } = new List<SectorAssessment>();

    public ICollection<Bottleneck> Bottlenecks { get; set; } = new List<Bottleneck>();

    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

    public ICollection<Phase> Phases { get; set; } = new List<Phase>();
}

public enum DiagnosisStatus
{
    Draft,
    InProgress,
    Completed
}
