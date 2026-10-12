namespace Prisma.Api.Features.Diagnoses;

public sealed class DiagnosisObjective
{
    public Guid DiagnosisId { get; set; }

    public string Code { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public string? CustomDescription { get; set; }

    public Diagnosis Diagnosis { get; set; } = null!;
}
