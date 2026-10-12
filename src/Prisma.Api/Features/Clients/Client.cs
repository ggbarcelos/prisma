using Prisma.Api.Features.Diagnoses;

namespace Prisma.Api.Features.Clients;

public sealed class Client
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CompanyName { get; set; } = string.Empty;

    public string Segment { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<ClientSector> Sectors { get; set; } = new List<ClientSector>();

    public ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();
}
