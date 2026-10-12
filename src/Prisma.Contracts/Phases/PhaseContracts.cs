namespace Prisma.Contracts.Phases;

public sealed record PhaseContract(
    Guid Id,
    Guid DiagnosisId,
    string Name,
    string? Description,
    int SortOrder);

public sealed record PhaseInput(string? Name, string? Description);

public sealed record PhaseOrderInput(IReadOnlyList<Guid>? PhaseIds);
