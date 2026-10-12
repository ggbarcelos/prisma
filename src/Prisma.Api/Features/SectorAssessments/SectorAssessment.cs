using Prisma.Api.Features.Clients;
using Prisma.Api.Features.Bottlenecks;
using Prisma.Api.Features.Diagnoses;

namespace Prisma.Api.Features.SectorAssessments;

public sealed class SectorAssessment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DiagnosisId { get; set; }

    public Guid ClientId { get; set; }

    public Guid ClientSectorId { get; set; }

    public string SectorNameSnapshot { get; set; } = string.Empty;

    public string? Answer01 { get; set; }

    public string? Answer02 { get; set; }

    public string? Answer03 { get; set; }

    public string? Answer04 { get; set; }

    public string? Answer05 { get; set; }

    public string? Answer06 { get; set; }

    public string? Answer07 { get; set; }

    public string? Answer08 { get; set; }

    public string? Answer09 { get; set; }

    public string? Answer10 { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public Diagnosis Diagnosis { get; set; } = null!;

    public ClientSector ClientSector { get; set; } = null!;

    public ICollection<Bottleneck> Bottlenecks { get; set; } = new List<Bottleneck>();
}
