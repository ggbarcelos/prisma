using Microsoft.EntityFrameworkCore;
using Prisma.Api.Data;
using Prisma.Api.Features.Diagnoses;
using Prisma.Contracts.Bottlenecks;

namespace Prisma.Api.Features.Bottlenecks;

public static class BottleneckEndpoints
{
    private static readonly HashSet<string> Categories = new(StringComparer.Ordinal)
    {
        "ManualWork",
        "Repetition",
        "Rework",
        "Delay",
        "MissingIntegration",
        "ScatteredInformation",
        "PeopleDependency",
        "CommercialProblem",
        "Other"
    };

    public static RouteGroupBuilder MapBottleneckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/diagnoses/{diagnosisId:guid}/bottlenecks")
            .WithTags("Bottlenecks");
        group.MapGet("", ListAsync).WithName("ListBottlenecks");
        group.MapPost("", CreateAsync).WithName("CreateBottleneck");
        group.MapPut("/{bottleneckId:guid}", UpdateAsync).WithName("UpdateBottleneck");
        group.MapDelete("/{bottleneckId:guid}", DeleteAsync).WithName("DeleteBottleneck");
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

        var bottlenecks = await dbContext.Bottlenecks
            .AsNoTracking()
            .Where(bottleneck => bottleneck.DiagnosisId == diagnosisId)
            .OrderByDescending(bottleneck => bottleneck.CreatedAtUtc)
            .ThenBy(bottleneck => bottleneck.Title)
            .ThenBy(bottleneck => bottleneck.Id)
            .ToListAsync(cancellationToken);

        return Results.Ok(bottlenecks.Select(ToContract));
    }

    private static async Task<IResult> CreateAsync(
        Guid diagnosisId,
        BottleneckInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors, title: "Revise os dados do gargalo.");
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
                title: "Reabra o diagnóstico antes de registrar gargalos.");
        }

        var assessmentExists = await dbContext.SectorAssessments
            .AnyAsync(
                assessment => assessment.Id == input.SectorAssessmentId && assessment.DiagnosisId == diagnosisId,
                cancellationToken);
        if (!assessmentExists)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Setor avaliado não encontrado.");
        }

        var now = DateTimeOffset.UtcNow;
        var bottleneck = new Bottleneck
        {
            DiagnosisId = diagnosis.Id,
            SectorAssessmentId = input.SectorAssessmentId,
            Title = input.Title!.Trim(),
            Description = NormalizeOptional(input.Description),
            Category = input.Category!,
            Impact = input.Impact,
            Frequency = NormalizeOptional(input.Frequency),
            Evidence = NormalizeOptional(input.Evidence),
            ProbableCause = NormalizeOptional(input.ProbableCause),
            EstimatedMonthlyHours = input.EstimatedMonthlyHours,
            Notes = NormalizeOptional(input.Notes),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        diagnosis.UpdatedAtUtc = now;
        dbContext.Bottlenecks.Add(bottleneck);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Não foi possível salvar o gargalo.");
        }

        return Results.Created(
            $"/api/diagnoses/{diagnosis.Id}/bottlenecks/{bottleneck.Id}",
            ToContract(bottleneck));
    }

    private static async Task<IResult> UpdateAsync(
        Guid diagnosisId,
        Guid bottleneckId,
        BottleneckInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors, title: "Revise os dados do gargalo.");
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
                title: "Reabra o diagnóstico antes de editar gargalos.");
        }

        var bottleneck = await dbContext.Bottlenecks
            .SingleOrDefaultAsync(
                candidate => candidate.Id == bottleneckId && candidate.DiagnosisId == diagnosisId,
                cancellationToken);
        if (bottleneck is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Gargalo não encontrado.");
        }

        var assessmentExists = await dbContext.SectorAssessments
            .AnyAsync(
                assessment => assessment.Id == input.SectorAssessmentId && assessment.DiagnosisId == diagnosisId,
                cancellationToken);
        if (!assessmentExists)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Setor avaliado não encontrado.");
        }

        ApplyInput(bottleneck, input);
        var now = DateTimeOffset.UtcNow;
        bottleneck.UpdatedAtUtc = now;
        diagnosis.UpdatedAtUtc = now;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Não foi possível atualizar o gargalo.");
        }

        return Results.Ok(ToContract(bottleneck));
    }

    private static async Task<IResult> DeleteAsync(
        Guid diagnosisId,
        Guid bottleneckId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
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
                title: "Reabra o diagnóstico antes de excluir gargalos.");
        }

        var bottleneck = await dbContext.Bottlenecks
            .SingleOrDefaultAsync(
                candidate => candidate.Id == bottleneckId && candidate.DiagnosisId == diagnosisId,
                cancellationToken);
        if (bottleneck is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Gargalo não encontrado.");
        }

        diagnosis.UpdatedAtUtc = DateTimeOffset.UtcNow;
        dbContext.Bottlenecks.Remove(bottleneck);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Remova ou reassocie as oportunidades vinculadas antes de excluir este gargalo.");
        }

        return Results.NoContent();
    }

    private static Dictionary<string, string[]> Validate(BottleneckInput input)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            errors[nameof(input.Title)] = ["Informe o título do gargalo."];
        }

        if (string.IsNullOrWhiteSpace(input.Category) || !Categories.Contains(input.Category))
        {
            errors[nameof(input.Category)] = ["Selecione uma categoria válida."];
        }

        if (input.Impact is < 1 or > 5)
        {
            errors[nameof(input.Impact)] = ["O impacto deve estar entre 1 e 5."];
        }

        if (input.EstimatedMonthlyHours < 0)
        {
            errors[nameof(input.EstimatedMonthlyHours)] = ["As horas mensais estimadas não podem ser negativas."];
        }

        return errors;
    }

    private static void ApplyInput(Bottleneck bottleneck, BottleneckInput input)
    {
        bottleneck.SectorAssessmentId = input.SectorAssessmentId;
        bottleneck.Title = input.Title!.Trim();
        bottleneck.Description = NormalizeOptional(input.Description);
        bottleneck.Category = input.Category!;
        bottleneck.Impact = input.Impact;
        bottleneck.Frequency = NormalizeOptional(input.Frequency);
        bottleneck.Evidence = NormalizeOptional(input.Evidence);
        bottleneck.ProbableCause = NormalizeOptional(input.ProbableCause);
        bottleneck.EstimatedMonthlyHours = input.EstimatedMonthlyHours;
        bottleneck.Notes = NormalizeOptional(input.Notes);
    }

    private static BottleneckContract ToContract(Bottleneck bottleneck) => new(
        bottleneck.Id,
        bottleneck.DiagnosisId,
        bottleneck.SectorAssessmentId,
        bottleneck.Title,
        bottleneck.Description,
        bottleneck.Category,
        bottleneck.Impact,
        bottleneck.Frequency,
        bottleneck.Evidence,
        bottleneck.ProbableCause,
        bottleneck.EstimatedMonthlyHours,
        bottleneck.Notes);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
