namespace Prisma.Contracts.Clients;

public sealed record ClientInput(
    string? CompanyName,
    string? Segment,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Notes);

public sealed record ClientContract(
    Guid Id,
    string CompanyName,
    string Segment,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ClientSectorInput(
    string? Name,
    string? ResponsibleName,
    int? PeopleCount);

public sealed record ClientSectorContract(
    Guid Id,
    Guid ClientId,
    string Name,
    string? ResponsibleName,
    int? PeopleCount);
