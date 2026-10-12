using Microsoft.EntityFrameworkCore;
using Prisma.Api.Data;
using Prisma.Api.Features.Bottlenecks;
using Prisma.Api.Features.Diagnoses;
using Prisma.Contracts.Opportunities;

namespace Prisma.Api.Features.Opportunities;

public static class OpportunityEndpoints
{
    private static readonly HashSet<string> Types = new(StringComparer.Ordinal)
    {
        "ProcessImprovement",
        "ExistingTool",
        "Integration",
        "CustomSoftware",
        "Automation",
        "AiApplication"
    };

    public static RouteGroupBuilder MapOpportunityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/diagnoses/{diagnosisId:guid}/opportunities")
            .WithTags("Opportunities");
        group.MapGet("", ListAsync).WithName("ListOpportunities");
        group.MapPost("", CreateAsync).WithName("CreateOpportunity");
        group.MapPut("/{opportunityId:guid}", UpdateAsync).WithName("UpdateOpportunity");
        group.MapDelete("/{opportunityId:guid}", DeleteAsync).WithName("DeleteOpportunity");
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

        var opportunities = await dbContext.Opportunities
            .AsNoTracking()
            .Include(opportunity => opportunity.BottleneckLinks)
            .Where(opportunity => opportunity.DiagnosisId == diagnosisId)
            .ToListAsync(cancellationToken);
        var ordered = opportunities
            .OrderByDescending(PriorityScore)
            .ThenByDescending(opportunity => opportunity.Impact)
            .ThenBy(opportunity => opportunity.Effort)
            .ThenBy(opportunity => opportunity.Title, StringComparer.Ordinal)
            .ThenBy(opportunity => opportunity.Id)
            .Select(ToContract)
            .ToList();

        return Results.Ok(ordered);
    }

    private static async Task<IResult> CreateAsync(
        Guid diagnosisId,
        OpportunityInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors, title: "Revise os dados da oportunidade.");
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
                title: "Reabra o diagnóstico antes de registrar oportunidades.");
        }

        if (input.PhaseId.HasValue && !await dbContext.Phases
                .AnyAsync(phase => phase.Id == input.PhaseId.Value && phase.DiagnosisId == diagnosisId, cancellationToken))
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Fase não encontrada neste diagnóstico.");
        }

        var bottlenecks = await LoadOwnedBottlenecksAsync(
            dbContext, diagnosisId, input.BottleneckIds!, cancellationToken);
        if (bottlenecks is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Gargalo não encontrado neste diagnóstico.");
        }

        var now = DateTimeOffset.UtcNow;
        var opportunity = new Opportunity
        {
            DiagnosisId = diagnosisId,
            PhaseId = input.PhaseId,
            Title = input.Title!.Trim(),
            SolutionDescription = NormalizeOptional(input.SolutionDescription),
            Type = input.Type!,
            ExpectedBenefit = input.ExpectedBenefit!.Trim(),
            Impact = input.Impact,
            Effort = input.Effort,
            Urgency = input.Urgency,
            Dependencies = NormalizeOptional(input.Dependencies),
            AiDataNeeds = NormalizeOptional(input.AiDataNeeds),
            NeedsHumanReview = input.NeedsHumanReview,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        foreach (var bottleneck in bottlenecks)
        {
            opportunity.BottleneckLinks.Add(new OpportunityBottleneck
            {
                DiagnosisId = diagnosisId,
                OpportunityId = opportunity.Id,
                BottleneckId = bottleneck.Id,
                Opportunity = opportunity,
                Bottleneck = bottleneck
            });
        }

        diagnosis.UpdatedAtUtc = now;
        dbContext.Opportunities.Add(opportunity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Não foi possível salvar a oportunidade.");
        }

        return Results.Created(
            $"/api/diagnoses/{diagnosisId}/opportunities/{opportunity.Id}",
            ToContract(opportunity));
    }

    private static async Task<IResult> UpdateAsync(
        Guid diagnosisId,
        Guid opportunityId,
        OpportunityInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors, title: "Revise os dados da oportunidade.");
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
                title: "Reabra o diagnóstico antes de editar oportunidades.");
        }

        var opportunity = await dbContext.Opportunities
            .Include(candidate => candidate.BottleneckLinks)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == opportunityId && candidate.DiagnosisId == diagnosisId,
                cancellationToken);
        if (opportunity is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Oportunidade não encontrada.");
        }

        if (input.PhaseId.HasValue && !await dbContext.Phases
                .AnyAsync(phase => phase.Id == input.PhaseId.Value && phase.DiagnosisId == diagnosisId, cancellationToken))
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Fase não encontrada neste diagnóstico.");
        }

        var bottlenecks = await LoadOwnedBottlenecksAsync(
            dbContext, diagnosisId, input.BottleneckIds!, cancellationToken);
        if (bottlenecks is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Gargalo não encontrado neste diagnóstico.");
        }

        opportunity.Title = input.Title!.Trim();
        opportunity.SolutionDescription = NormalizeOptional(input.SolutionDescription);
        opportunity.Type = input.Type!;
        opportunity.ExpectedBenefit = input.ExpectedBenefit!.Trim();
        opportunity.Impact = input.Impact;
        opportunity.Effort = input.Effort;
        opportunity.Urgency = input.Urgency;
        opportunity.Dependencies = NormalizeOptional(input.Dependencies);
        opportunity.AiDataNeeds = NormalizeOptional(input.AiDataNeeds);
        opportunity.NeedsHumanReview = input.NeedsHumanReview;
        opportunity.PhaseId = input.PhaseId;

        var requestedIds = input.BottleneckIds!.ToHashSet();
        foreach (var existingLink in opportunity.BottleneckLinks.ToArray())
        {
            if (!requestedIds.Contains(existingLink.BottleneckId))
            {
                dbContext.OpportunityBottlenecks.Remove(existingLink);
            }
        }

        var existingIds = opportunity.BottleneckLinks
            .Where(link => requestedIds.Contains(link.BottleneckId))
            .Select(link => link.BottleneckId)
            .ToHashSet();
        foreach (var bottleneck in bottlenecks.Where(bottleneck => !existingIds.Contains(bottleneck.Id)))
        {
            opportunity.BottleneckLinks.Add(new OpportunityBottleneck
            {
                DiagnosisId = diagnosisId,
                OpportunityId = opportunity.Id,
                BottleneckId = bottleneck.Id,
                Opportunity = opportunity,
                Bottleneck = bottleneck
            });
        }

        var now = DateTimeOffset.UtcNow;
        opportunity.UpdatedAtUtc = now;
        diagnosis.UpdatedAtUtc = now;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Não foi possível atualizar a oportunidade.");
        }

        return Results.Ok(ToContract(opportunity));
    }

    private static async Task<IResult> DeleteAsync(
        Guid diagnosisId,
        Guid opportunityId,
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
                title: "Reabra o diagnóstico antes de excluir oportunidades.");
        }

        var opportunity = await dbContext.Opportunities
            .SingleOrDefaultAsync(
                candidate => candidate.Id == opportunityId && candidate.DiagnosisId == diagnosisId,
                cancellationToken);
        if (opportunity is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Oportunidade não encontrada.");
        }

        diagnosis.UpdatedAtUtc = DateTimeOffset.UtcNow;
        dbContext.Opportunities.Remove(opportunity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Não foi possível excluir a oportunidade.");
        }

        return Results.NoContent();
    }

    private static Dictionary<string, string[]> Validate(OpportunityInput input)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            errors[nameof(input.Title)] = ["Informe o título da oportunidade."];
        }

        if (string.IsNullOrWhiteSpace(input.Type) || !Types.Contains(input.Type))
        {
            errors[nameof(input.Type)] = ["Selecione um tipo de oportunidade válido."];
        }

        if (string.IsNullOrWhiteSpace(input.ExpectedBenefit))
        {
            errors[nameof(input.ExpectedBenefit)] = ["Informe o benefício esperado."];
        }

        if (input.Impact is < 1 or > 5)
        {
            errors[nameof(input.Impact)] = ["O impacto deve estar entre 1 e 5."];
        }

        if (input.Effort is < 1 or > 5)
        {
            errors[nameof(input.Effort)] = ["O esforço deve estar entre 1 e 5."];
        }

        if (input.Urgency is < 1 or > 5)
        {
            errors[nameof(input.Urgency)] = ["A urgência deve estar entre 1 e 5."];
        }

        if (input.BottleneckIds is null || input.BottleneckIds.Count == 0)
        {
            errors[nameof(input.BottleneckIds)] = ["Associe ao menos um gargalo à oportunidade."];
        }
        else if (input.BottleneckIds.Distinct().Count() != input.BottleneckIds.Count)
        {
            errors[nameof(input.BottleneckIds)] = ["Não repita gargalos na associação."];
        }

        return errors;
    }

    private static async Task<List<Bottleneck>?> LoadOwnedBottlenecksAsync(
        AppDbContext dbContext,
        Guid diagnosisId,
        IReadOnlyList<Guid> bottleneckIds,
        CancellationToken cancellationToken)
    {
        var bottlenecks = await dbContext.Bottlenecks
            .Where(bottleneck => bottleneck.DiagnosisId == diagnosisId && bottleneckIds.Contains(bottleneck.Id))
            .ToListAsync(cancellationToken);
        return bottlenecks.Count == bottleneckIds.Count ? bottlenecks : null;
    }

    private static int PriorityScore(Opportunity opportunity) =>
        (opportunity.Impact * 2) + opportunity.Urgency - opportunity.Effort;

    private static OpportunityContract ToContract(Opportunity opportunity) => new(
        opportunity.Id,
        opportunity.DiagnosisId,
        opportunity.Title,
        opportunity.SolutionDescription,
        opportunity.Type,
        opportunity.ExpectedBenefit,
        opportunity.Impact,
        opportunity.Effort,
        opportunity.Urgency,
        opportunity.Dependencies,
        opportunity.AiDataNeeds,
        opportunity.NeedsHumanReview,
        opportunity.PhaseId,
        opportunity.BottleneckLinks.Select(link => link.BottleneckId).OrderBy(id => id).ToArray(),
        PriorityScore(opportunity));

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
