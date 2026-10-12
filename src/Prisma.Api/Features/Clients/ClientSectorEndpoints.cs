using Microsoft.EntityFrameworkCore;
using Prisma.Api.Data;
using Prisma.Contracts.Clients;

namespace Prisma.Api.Features.Clients;

public static class ClientSectorEndpoints
{
    public static RouteGroupBuilder MapClientSectorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/clients/{clientId:guid}/sectors").WithTags("ClientSectors");
        group.MapGet("", ListAsync).WithName("ListClientSectors");
        group.MapPost("", CreateAsync).WithName("CreateClientSector");
        group.MapPut("/{sectorId:guid}", UpdateAsync).WithName("UpdateClientSector");
        return group;
    }

    private static async Task<IResult> ListAsync(
        Guid clientId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Clients.AnyAsync(client => client.Id == clientId, cancellationToken))
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Cliente não encontrado.");
        }

        var sectors = await dbContext.ClientSectors
            .AsNoTracking()
            .Where(sector => sector.ClientId == clientId)
            .OrderBy(sector => sector.Name)
            .ThenBy(sector => sector.Id)
            .Select(sector => ToContract(sector))
            .ToListAsync(cancellationToken);

        return Results.Ok(sectors);
    }

    private static async Task<IResult> CreateAsync(
        Guid clientId,
        ClientSectorInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors, title: "Revise os dados do setor.");
        }

        var client = await dbContext.Clients
            .SingleOrDefaultAsync(candidate => candidate.Id == clientId, cancellationToken);
        if (client is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Cliente não encontrado.");
        }

        var now = DateTimeOffset.UtcNow;
        var sector = new ClientSector
        {
            ClientId = clientId,
            Name = input.Name!.Trim(),
            ResponsibleName = NormalizeOptional(input.ResponsibleName),
            PeopleCount = input.PeopleCount,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        client.UpdatedAtUtc = now;
        dbContext.ClientSectors.Add(sector);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível salvar o setor.");
        }

        return Results.Created(
            $"/api/clients/{clientId}/sectors/{sector.Id}",
            ToContract(sector));
    }

    private static async Task<IResult> UpdateAsync(
        Guid clientId,
        Guid sectorId,
        ClientSectorInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors, title: "Revise os dados do setor.");
        }

        var sector = await dbContext.ClientSectors
            .SingleOrDefaultAsync(candidate => candidate.Id == sectorId && candidate.ClientId == clientId, cancellationToken);
        if (sector is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Setor não encontrado.");
        }

        var client = await dbContext.Clients
            .SingleAsync(candidate => candidate.Id == clientId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        sector.Name = input.Name!.Trim();
        sector.ResponsibleName = NormalizeOptional(input.ResponsibleName);
        sector.PeopleCount = input.PeopleCount;
        sector.UpdatedAtUtc = now;
        client.UpdatedAtUtc = now;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível atualizar o setor.");
        }

        return Results.Ok(ToContract(sector));
    }

    private static Dictionary<string, string[]> Validate(ClientSectorInput input)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            errors[nameof(input.Name)] = ["Informe o nome do setor."];
        }

        return errors;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ClientSectorContract ToContract(ClientSector sector) => new(
        sector.Id,
        sector.ClientId,
        sector.Name,
        sector.ResponsibleName,
        sector.PeopleCount);
}
