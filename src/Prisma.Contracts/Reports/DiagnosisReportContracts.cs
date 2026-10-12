using Prisma.Contracts.Bottlenecks;
using Prisma.Contracts.Clients;
using Prisma.Contracts.Opportunities;
using Prisma.Contracts.Phases;
using Prisma.Contracts.SectorAssessments;

namespace Prisma.Contracts.Reports;

public sealed record DiagnosisReportInput(
    string? ExecutiveSummary,
    string? GeneralRecommendations,
    string? NextSteps);

public sealed record DiagnosisReportObjectiveContract(
    string Code,
    bool IsPrimary,
    string? CustomDescription);

public sealed record DiagnosisReportContract(
    Guid DiagnosisId,
    string DiagnosisTitle,
    DateOnly AssessmentDate,
    string? Context,
    string Status,
    ClientContract Client,
    IReadOnlyList<DiagnosisReportObjectiveContract> Objectives,
    string? ExecutiveSummary,
    string? GeneralRecommendations,
    IReadOnlyList<SectorAssessmentContract> Assessments,
    IReadOnlyList<BottleneckContract> Bottlenecks,
    IReadOnlyList<OpportunityContract> Opportunities,
    IReadOnlyList<PhaseContract> Phases,
    IReadOnlyList<OpportunityContract> UnallocatedOpportunities,
    string? NextSteps);
