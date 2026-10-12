using Microsoft.EntityFrameworkCore;
using Prisma.Api.Data;
using Prisma.Api.Features.Diagnoses;
using Prisma.Contracts.Phases;

namespace Prisma.Api.Features.Phases;

public static class PhaseEndpoints
{
    public static RouteGroupBuilder MapPhaseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/diagnoses/{diagnosisId:guid}/phases")
            .WithTags("Phases");
        group.MapGet("", ListAsync).WithName("ListPhases");
        group.MapPost("", CreateAsync).WithName("CreatePhase");
        group.MapPut("/{phaseId:guid}", UpdateAsync).WithName("UpdatePhase");
        group.MapPut("/order", UpdateOrderAsync).WithName("UpdatePhaseOrder");
        return group;
    }

    private static async Task<IResult> ListAsync(
        Guid diagnosisId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var diagnosisExists = await dbContext.Diagnoses
            .AnyAsync(diagnosis => diagnosis.Id == diagnosisId, cancellationToken);
        if (!diagnosisExists)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Diagnóstico não encontrado.");
        }

        var phases = await dbContext.Phases
            .AsNoTracking()
            .Where(phase => phase.DiagnosisId == diagnosisId)
            .OrderBy(phase => phase.SortOrder)
            .ThenBy(phase => phase.Id)
            .Select(phase => ToContract(phase))
            .ToListAsync(cancellationToken);
        return Results.Ok(phases);
    }

    private static async Task<IResult> CreateAsync(
        Guid diagnosisId,
        PhaseInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors, title: "Revise os dados da fase.");
        }

        var diagnosis = await dbContext.Diagnoses
            .SingleOrDefaultAsync(candidate => candidate.Id == diagnosisId, cancellationToken);
        if (diagnosis is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Diagnóstico não encontrado.");
        }

        if (diagnosis.Status == DiagnosisStatus.Completed)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Reabra o diagnóstico antes de adicionar fases.");
        }

        var currentMaxOrder = await dbContext.Phases
            .Where(phase => phase.DiagnosisId == diagnosisId)
            .Select(phase => (int?)phase.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;
        if (currentMaxOrder == int.MaxValue)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Não há mais posições disponíveis para fases.");
        }

        var now = DateTimeOffset.UtcNow;
        var phase = new Phase
        {
            DiagnosisId = diagnosisId,
            Name = input.Name!.Trim(),
            Description = NormalizeOptional(input.Description),
            SortOrder = currentMaxOrder + 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        diagnosis.UpdatedAtUtc = now;
        dbContext.Phases.Add(phase);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Não foi possível salvar a fase.");
        }

        return Results.Created($"/api/diagnoses/{diagnosisId}/phases", ToContract(phase));
    }

    private static async Task<IResult> UpdateAsync(
        Guid diagnosisId,
        Guid phaseId,
        PhaseInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors, title: "Revise os dados da fase.");
        }

        var diagnosis = await dbContext.Diagnoses
            .SingleOrDefaultAsync(candidate => candidate.Id == diagnosisId, cancellationToken);
        if (diagnosis is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Diagnóstico não encontrado.");
        }

        if (diagnosis.Status == DiagnosisStatus.Completed)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Reabra o diagnóstico antes de editar fases.");
        }

        var phase = await dbContext.Phases
            .SingleOrDefaultAsync(
                candidate => candidate.Id == phaseId && candidate.DiagnosisId == diagnosisId,
                cancellationToken);
        if (phase is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Fase não encontrada neste diagnóstico.");
        }

        var now = DateTimeOffset.UtcNow;
        phase.Name = input.Name!.Trim();
        phase.Description = NormalizeOptional(input.Description);
        phase.UpdatedAtUtc = now;
        diagnosis.UpdatedAtUtc = now;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Não foi possível atualizar a fase.");
        }

        return Results.Ok(ToContract(phase));
    }

    private static async Task<IResult> UpdateOrderAsync(
        Guid diagnosisId,
        PhaseOrderInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (input.PhaseIds is null)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    [nameof(input.PhaseIds)] = ["Informe a lista completa e sem repetições das fases."]
                },
                title: "Revise a ordem das fases.");
        }

        var diagnosis = await dbContext.Diagnoses
            .SingleOrDefaultAsync(candidate => candidate.Id == diagnosisId, cancellationToken);
        if (diagnosis is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Diagnóstico não encontrado.");
        }

        if (diagnosis.Status == DiagnosisStatus.Completed)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Reabra o diagnóstico antes de reordenar fases.");
        }

        var phases = await dbContext.Phases
            .AsNoTracking()
            .Where(phase => phase.DiagnosisId == diagnosisId)
            .ToListAsync(cancellationToken);
        var requestedIds = input.PhaseIds.ToHashSet();
        var existingIds = phases.Select(phase => phase.Id).ToHashSet();
        if (input.PhaseIds.Count != phases.Count || requestedIds.Count != input.PhaseIds.Count ||
            !requestedIds.SetEquals(existingIds))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    [nameof(input.PhaseIds)] = ["A ordem deve conter cada fase deste diagnóstico exatamente uma vez."]
                },
                title: "Revise a ordem das fases.");
        }

        if (phases.Count == 0)
        {
            return Results.Ok(Array.Empty<PhaseContract>());
        }

        var offset = phases.Count;
        if (phases.Any(phase => phase.SortOrder > int.MaxValue - offset))
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Não foi possível normalizar a ordem das fases.");
        }

        var now = DateTimeOffset.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.Phases
                .Where(phase => phase.DiagnosisId == diagnosisId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(phase => phase.SortOrder, phase => phase.SortOrder + offset)
                        .SetProperty(phase => phase.UpdatedAtUtc, now),
                    cancellationToken);

            for (var sortOrder = 0; sortOrder < input.PhaseIds.Count; sortOrder++)
            {
                var phaseId = input.PhaseIds[sortOrder];
                await dbContext.Phases
                    .Where(phase => phase.DiagnosisId == diagnosisId && phase.Id == phaseId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(phase => phase.SortOrder, sortOrder),
                        cancellationToken);
            }

            diagnosis.UpdatedAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Não foi possível reordenar as fases.");
        }

        var reordered = await dbContext.Phases
            .AsNoTracking()
            .Where(phase => phase.DiagnosisId == diagnosisId)
            .OrderBy(phase => phase.SortOrder)
            .ThenBy(phase => phase.Id)
            .Select(phase => ToContract(phase))
            .ToListAsync(cancellationToken);
        return Results.Ok(reordered);
    }

    private static Dictionary<string, string[]> Validate(PhaseInput input)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            errors[nameof(input.Name)] = ["Informe o nome da fase."];
        }

        return errors;
    }

    private static PhaseContract ToContract(Phase phase) => new(
        phase.Id,
        phase.DiagnosisId,
        phase.Name,
        phase.Description,
        phase.SortOrder);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
