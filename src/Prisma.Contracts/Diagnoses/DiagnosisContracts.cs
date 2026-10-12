using Prisma.Contracts.SectorAssessments;

namespace Prisma.Contracts.Diagnoses;

public sealed record ObjectiveInput(
    string? Code,
    bool IsPrimary,
    string? CustomDescription);

public sealed record DiagnosisInput(
    Guid ClientId,
    string? Title,
    DateOnly AssessmentDate,
    string? Context,
    IReadOnlyList<ObjectiveInput>? Objectives);

public sealed record DiagnosisStatusInput(string? Status);

public sealed record DiagnosisSummaryContract(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string Title,
    DateOnly AssessmentDate,
    string Status,
    DateTimeOffset UpdatedAtUtc);

public sealed record DiagnosisContract(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string Title,
    DateOnly AssessmentDate,
    string Status,
    DateTimeOffset UpdatedAtUtc,
    string? Context,
    IReadOnlyList<ObjectiveInput> Objectives,
    IReadOnlyList<SectorAssessmentContract> Assessments);
