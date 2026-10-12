namespace Prisma.Contracts.Opportunities;

public sealed record OpportunityInput(
    string? Title,
    string? SolutionDescription,
    string? Type,
    string? ExpectedBenefit,
    int Impact,
    int Effort,
    int Urgency,
    string? Dependencies,
    string? AiDataNeeds,
    bool? NeedsHumanReview,
    Guid? PhaseId,
    IReadOnlyList<Guid>? BottleneckIds);

public sealed record OpportunityContract(
    Guid Id,
    Guid DiagnosisId,
    string Title,
    string? SolutionDescription,
    string Type,
    string ExpectedBenefit,
    int Impact,
    int Effort,
    int Urgency,
    string? Dependencies,
    string? AiDataNeeds,
    bool? NeedsHumanReview,
    Guid? PhaseId,
    IReadOnlyList<Guid> BottleneckIds,
    int PriorityScore);
