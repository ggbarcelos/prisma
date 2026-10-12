using Prisma.Api.Features.Diagnoses;
using Prisma.Api.Features.Opportunities;

namespace Prisma.Api.Features.Phases;

public sealed class Phase
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DiagnosisId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public Diagnosis Diagnosis { get; set; } = null!;

    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
}
