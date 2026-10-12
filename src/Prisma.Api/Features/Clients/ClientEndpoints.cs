using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Prisma.Api.Data;
using Prisma.Contracts.Clients;

namespace Prisma.Api.Features.Clients;

public static class ClientEndpoints
{
    public static RouteGroupBuilder MapClientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/clients").WithTags("Clients");
        group.MapGet("", ListAsync).WithName("ListClients");
        group.MapPost("", CreateAsync).WithName("CreateClient");
        group.MapGet("/{clientId:guid}", GetAsync).WithName("GetClient");
        group.MapPut("/{clientId:guid}", UpdateAsync).WithName("UpdateClient");
        return group;
    }

    private static async Task<IResult> ListAsync(
        string? search,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        IQueryable<Client> query = dbContext.Clients.AsNoTracking();
        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var pattern = $"%{EscapeLikePattern(term)}%";
            query = query.Where(client =>
                EF.Functions.ILike(client.CompanyName, pattern) ||
                EF.Functions.ILike(client.Segment, pattern) ||
                (client.ContactName != null && EF.Functions.ILike(client.ContactName, pattern)));
        }

        var clients = await query
            .OrderByDescending(client => client.UpdatedAtUtc)
            .ThenBy(client => client.CompanyName)
            .Select(client => ToContract(client))
            .ToListAsync(cancellationToken);

        return Results.Ok(clients);
    }

    private static async Task<IResult> GetAsync(
        Guid clientId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == clientId, cancellationToken);

        return client is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Cliente não encontrado.")
            : Results.Ok(ToContract(client));
    }

    private static async Task<IResult> CreateAsync(
        ClientInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors, title: "Revise os dados do cliente.");
        }

        var now = DateTimeOffset.UtcNow;
        var client = new Client
        {
            CompanyName = input.CompanyName!.Trim(),
            Segment = input.Segment!.Trim(),
            ContactName = NormalizeOptional(input.ContactName),
            Email = NormalizeOptional(input.Email),
            Phone = NormalizeOptional(input.Phone),
            Notes = NormalizeOptional(input.Notes),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Clients.Add(client);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível salvar o cliente.");
        }

        return Results.Created($"/api/clients/{client.Id}", ToContract(client));
    }

    private static async Task<IResult> UpdateAsync(
        Guid clientId,
        ClientInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors, title: "Revise os dados do cliente.");
        }

        var client = await dbContext.Clients
            .SingleOrDefaultAsync(candidate => candidate.Id == clientId, cancellationToken);
        if (client is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Cliente não encontrado.");
        }

        client.CompanyName = input.CompanyName!.Trim();
        client.Segment = input.Segment!.Trim();
        client.ContactName = NormalizeOptional(input.ContactName);
        client.Email = NormalizeOptional(input.Email);
        client.Phone = NormalizeOptional(input.Phone);
        client.Notes = NormalizeOptional(input.Notes);
        client.UpdatedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível atualizar o cliente.");
        }

        return Results.Ok(ToContract(client));
    }

    private static Dictionary<string, string[]> Validate(ClientInput input)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(input.CompanyName))
        {
            errors[nameof(input.CompanyName)] = ["Informe o nome da empresa."];
        }

        if (string.IsNullOrWhiteSpace(input.Segment))
        {
            errors[nameof(input.Segment)] = ["Informe o segmento."];
        }

        if (!string.IsNullOrWhiteSpace(input.Email) && !new EmailAddressAttribute().IsValid(input.Email.Trim()))
        {
            errors[nameof(input.Email)] = ["Informe um e-mail válido."];
        }

        return errors;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private static ClientContract ToContract(Client client) => new(
        client.Id,
        client.CompanyName,
        client.Segment,
        client.ContactName,
        client.Email,
        client.Phone,
        client.Notes,
        client.CreatedAtUtc,
        client.UpdatedAtUtc);
}
