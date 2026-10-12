namespace Prisma.Contracts.Bottlenecks;

public sealed record BottleneckInput(
    Guid SectorAssessmentId,
    string? Title,
    string? Description,
    string? Category,
    int Impact,
    string? Frequency,
    string? Evidence,
    string? ProbableCause,
    decimal? EstimatedMonthlyHours,
    string? Notes);

public sealed record BottleneckContract(
    Guid Id,
    Guid DiagnosisId,
    Guid SectorAssessmentId,
    string Title,
    string? Description,
    string Category,
    int Impact,
    string? Frequency,
    string? Evidence,
    string? ProbableCause,
    decimal? EstimatedMonthlyHours,
    string? Notes);
