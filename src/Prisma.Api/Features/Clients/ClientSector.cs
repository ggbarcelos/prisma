namespace Prisma.Api.Features.Clients;

public sealed class ClientSector
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClientId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ResponsibleName { get; set; }

    public int? PeopleCount { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public Client Client { get; set; } = null!;
}
