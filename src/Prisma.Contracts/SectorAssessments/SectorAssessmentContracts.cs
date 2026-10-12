namespace Prisma.Contracts.SectorAssessments;

public sealed record SectorAssessmentInput(Guid ClientSectorId);

public sealed record AnswersInput(
    string? Q01,
    string? Q02,
    string? Q03,
    string? Q04,
    string? Q05,
    string? Q06,
    string? Q07,
    string? Q08,
    string? Q09,
    string? Q10);

public sealed record SectorAssessmentContract(
    Guid Id,
    Guid DiagnosisId,
    Guid ClientSectorId,
    string SectorNameSnapshot,
    string? Q01,
    string? Q02,
    string? Q03,
    string? Q04,
    string? Q05,
    string? Q06,
    string? Q07,
    string? Q08,
    string? Q09,
    string? Q10,
    int AnsweredCount);
