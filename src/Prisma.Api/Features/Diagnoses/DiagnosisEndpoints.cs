using Microsoft.EntityFrameworkCore;
using Prisma.Api.Data;
using Prisma.Api.Features.Phases;
using Prisma.Api.Features.SectorAssessments;
using Prisma.Contracts.Diagnoses;
using Prisma.Contracts.SectorAssessments;

namespace Prisma.Api.Features.Diagnoses;

public static class DiagnosisEndpoints
{
    private static readonly HashSet<string> ObjectiveCodes = new(StringComparer.Ordinal)
    {
        "ReduceCosts",
        "ImproveProductivity",
        "ReduceErrorsAndRework",
        "ImproveService",
        "IncreaseSales",
        "IntegrateInformation",
        "Other"
    };

    public static RouteGroupBuilder MapDiagnosisEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/diagnoses").WithTags("Diagnoses");
        group.MapGet("", ListAsync).WithName("ListDiagnoses");
        group.MapPost("", CreateAsync).WithName("CreateDiagnosis");
        group.MapGet("/{diagnosisId:guid}", GetAsync).WithName("GetDiagnosis");
        group.MapPut("/{diagnosisId:guid}", UpdateAsync).WithName("UpdateDiagnosis");
        group.MapPut("/{diagnosisId:guid}/status", UpdateStatusAsync).WithName("UpdateDiagnosisStatus");
        group.MapPost("/{diagnosisId:guid}/assessments", AddAssessmentAsync).WithName("AddSectorAssessment");
        group.MapDelete("/{diagnosisId:guid}/assessments/{assessmentId:guid}", RemoveAssessmentAsync)
            .WithName("RemoveSectorAssessment");
        return group;
    }

    private static async Task<IResult> ListAsync(
        Guid? clientId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Diagnoses
            .AsNoTracking()
            .Include(diagnosis => diagnosis.Client)
            .AsQueryable();
        if (clientId.HasValue)
        {
            query = query.Where(diagnosis => diagnosis.ClientId == clientId.Value);
        }

        var items = await query
            .OrderByDescending(diagnosis => diagnosis.UpdatedAtUtc)
            .ThenBy(diagnosis => diagnosis.Title)
            .ThenBy(diagnosis => diagnosis.Id)
            .Select(diagnosis => new
            {
                diagnosis.Id,
                diagnosis.ClientId,
                ClientName = diagnosis.Client.CompanyName,
                diagnosis.Title,
                diagnosis.AssessmentDate,
                diagnosis.Status,
                diagnosis.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var diagnoses = items.Select(item => new DiagnosisSummaryContract(
            item.Id,
            item.ClientId,
            item.ClientName,
            item.Title,
            item.AssessmentDate,
            item.Status.ToString(),
            item.UpdatedAtUtc));

        return Results.Ok(diagnoses);
    }

    private static async Task<IResult> GetAsync(
        Guid diagnosisId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var diagnosis = await LoadDiagnosisAsync(dbContext, diagnosisId, cancellationToken);
        return diagnosis is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Diagnóstico não encontrado.")
            : Results.Ok(ToContract(diagnosis));
    }

    private static async Task<IResult> CreateAsync(
        DiagnosisInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors, title: "Revise os dados do diagnóstico.");
        }

        var client = await dbContext.Clients
            .SingleOrDefaultAsync(candidate => candidate.Id == input.ClientId, cancellationToken);
        if (client is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Cliente não encontrado.");
        }

        var now = DateTimeOffset.UtcNow;
        var diagnosis = new Diagnosis
        {
            ClientId = client.Id,
            Title = input.Title!.Trim(),
            AssessmentDate = input.AssessmentDate,
            Context = NormalizeOptional(input.Context),
            Status = DiagnosisStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        AddObjectives(diagnosis, input.Objectives!);
        AddSuggestedPhases(diagnosis, now);
        client.UpdatedAtUtc = now;
        dbContext.Diagnoses.Add(diagnosis);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível salvar o diagnóstico.");
        }

        diagnosis.Client = client;
        return Results.Created($"/api/diagnoses/{diagnosis.Id}", ToContract(diagnosis));
    }

    private static async Task<IResult> UpdateAsync(
        Guid diagnosisId,
        DiagnosisInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors, title: "Revise os dados do diagnóstico.");
        }

        var diagnosis = await dbContext.Diagnoses
            .Include(candidate => candidate.Objectives)
            .Include(candidate => candidate.Client)
            .Include(candidate => candidate.Assessments)
            .SingleOrDefaultAsync(candidate => candidate.Id == diagnosisId, cancellationToken);
        if (diagnosis is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Diagnóstico não encontrado.");
        }

        if (diagnosis.Status == DiagnosisStatus.Completed)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Reabra o diagnóstico antes de editá-lo.");
        }

        var clientChanged = diagnosis.ClientId != input.ClientId;
        if (clientChanged && await dbContext.SectorAssessments
                .AnyAsync(assessment => assessment.DiagnosisId == diagnosisId, cancellationToken))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "O cliente não pode ser alterado após a primeira avaliação de setor.");
        }

        var client = await dbContext.Clients
            .SingleOrDefaultAsync(candidate => candidate.Id == input.ClientId, cancellationToken);
        if (client is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Cliente não encontrado.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (clientChanged)
            {
                foreach (var objective in diagnosis.Objectives.ToArray())
                {
                    dbContext.Entry(objective).State = EntityState.Detached;
                }

                dbContext.Entry(diagnosis).State = EntityState.Detached;
                var affectedRows = await dbContext.Diagnoses
                    .Where(candidate => candidate.Id == diagnosisId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(candidate => candidate.ClientId, client.Id),
                        cancellationToken);
                if (affectedRows == 0)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Diagnóstico não encontrado.");
                }

                diagnosis = await dbContext.Diagnoses
                    .Include(candidate => candidate.Client)
                    .Include(candidate => candidate.Objectives)
                    .Include(candidate => candidate.Assessments)
                    .SingleOrDefaultAsync(candidate => candidate.Id == diagnosisId, cancellationToken);
                if (diagnosis is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Diagnóstico não encontrado.");
                }
            }

            diagnosis.Title = input.Title!.Trim();
            diagnosis.AssessmentDate = input.AssessmentDate;
            diagnosis.Context = NormalizeOptional(input.Context);
            diagnosis.UpdatedAtUtc = DateTimeOffset.UtcNow;
            var primaryCode = ReplaceObjectives(diagnosis, input.Objectives!);

            await dbContext.SaveChangesAsync(cancellationToken);
            if (primaryCode is not null)
            {
                diagnosis.Objectives.Single(objective => objective.Code == primaryCode).IsPrimary = true;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível atualizar o diagnóstico.");
        }

        return Results.Ok(ToContract(diagnosis));
    }

    private static async Task<IResult> AddAssessmentAsync(
        Guid diagnosisId,
        SectorAssessmentInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var diagnosis = await dbContext.Diagnoses
            .Include(candidate => candidate.Client)
            .SingleOrDefaultAsync(candidate => candidate.Id == diagnosisId, cancellationToken);
        if (diagnosis is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Diagnóstico não encontrado.");
        }

        if (diagnosis.Status == DiagnosisStatus.Completed)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Reabra o diagnóstico antes de adicionar uma avaliação.");
        }

        var sector = await dbContext.ClientSectors
            .SingleOrDefaultAsync(
                candidate => candidate.Id == input.ClientSectorId && candidate.ClientId == diagnosis.ClientId,
                cancellationToken);
        if (sector is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Setor do cliente não encontrado.");
        }

        var now = DateTimeOffset.UtcNow;
        var assessment = new SectorAssessment
        {
            DiagnosisId = diagnosis.Id,
            ClientId = diagnosis.ClientId,
            ClientSectorId = sector.Id,
            SectorNameSnapshot = sector.Name,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        diagnosis.UpdatedAtUtc = now;
        dbContext.SectorAssessments.Add(assessment);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "O setor já foi avaliado ou não pode ser associado a este diagnóstico.");
        }

        return Results.Created(
            $"/api/diagnoses/{diagnosisId}/assessments/{assessment.Id}",
            ToContract(assessment));
    }

    private static async Task<IResult> UpdateStatusAsync(
        Guid diagnosisId,
        DiagnosisStatusInput input,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (input.Status is not ("Draft" or "InProgress" or "Completed"))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { [nameof(input.Status)] = ["Escolha Rascunho, Em andamento ou Concluído."] },
                title: "O status informado não pode ser usado nesta etapa.");
        }

        var diagnosis = await dbContext.Diagnoses
            .Include(candidate => candidate.Client)
            .SingleOrDefaultAsync(candidate => candidate.Id == diagnosisId, cancellationToken);
        if (diagnosis is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Diagnóstico não encontrado.");
        }

        if (diagnosis.Status == DiagnosisStatus.Completed && input.Status != "InProgress")
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Um diagnóstico concluído só pode ser reaberto para Em andamento.");
        }

        if (input.Status == "Completed" && diagnosis.Status != DiagnosisStatus.Completed)
        {
            var hasPrimaryObjective = await dbContext.DiagnosisObjectives
                .AnyAsync(objective => objective.DiagnosisId == diagnosisId && objective.IsPrimary, cancellationToken);
            var hasBottleneck = await dbContext.Bottlenecks
                .AnyAsync(bottleneck => bottleneck.DiagnosisId == diagnosisId, cancellationToken);
            var hasOpportunity = await dbContext.Opportunities
                .AnyAsync(opportunity => opportunity.DiagnosisId == diagnosisId, cancellationToken);
            var missing = new List<string>();
            if (!hasPrimaryObjective)
            {
                missing.Add("objetivo principal");
            }

            if (!hasBottleneck)
            {
                missing.Add("ao menos um gargalo");
            }

            if (!hasOpportunity)
            {
                missing.Add("ao menos uma oportunidade");
            }

            if (missing.Count > 0)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "O diagnóstico não pode ser concluído.",
                    detail: $"Pendências para conclusão: {string.Join(", ", missing)}.");
            }
        }

        var status = input.Status switch
        {
            "Draft" => DiagnosisStatus.Draft,
            "InProgress" => DiagnosisStatus.InProgress,
            "Completed" => DiagnosisStatus.Completed,
            _ => throw new InvalidOperationException("The status was validated before mapping.")
        };
        if (diagnosis.Status != status)
        {
            diagnosis.Status = status;
            diagnosis.UpdatedAtUtc = DateTimeOffset.UtcNow;
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Não foi possível alterar o status do diagnóstico.");
            }
        }

        return Results.Ok(new DiagnosisSummaryContract(
            diagnosis.Id,
            diagnosis.ClientId,
            diagnosis.Client.CompanyName,
            diagnosis.Title,
            diagnosis.AssessmentDate,
            diagnosis.Status.ToString(),
            diagnosis.UpdatedAtUtc));
    }

    private static async Task<IResult> RemoveAssessmentAsync(
        Guid diagnosisId,
        Guid assessmentId,
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
                title: "Reabra o diagnóstico antes de remover avaliações.");
        }

        var assessment = await dbContext.SectorAssessments
            .SingleOrDefaultAsync(
                candidate => candidate.Id == assessmentId && candidate.DiagnosisId == diagnosisId,
                cancellationToken);
        if (assessment is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Setor avaliado não encontrado.");
        }

        if (AnsweredCount(assessment) > 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não é possível remover um setor avaliado que já tenha respostas.");
        }

        var hasBottlenecks = await dbContext.Bottlenecks
            .AnyAsync(bottleneck => bottleneck.DiagnosisId == diagnosisId && bottleneck.SectorAssessmentId == assessmentId,
                cancellationToken);
        if (hasBottlenecks)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Resolva os gargalos do setor antes de remover a avaliação.");
        }

        diagnosis.UpdatedAtUtc = DateTimeOffset.UtcNow;
        dbContext.SectorAssessments.Remove(assessment);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível remover o setor avaliado.");
        }

        return Results.NoContent();
    }

    private static async Task<Diagnosis?> LoadDiagnosisAsync(
        AppDbContext dbContext,
        Guid diagnosisId,
        CancellationToken cancellationToken) =>
        await dbContext.Diagnoses
            .AsNoTracking()
            .Include(diagnosis => diagnosis.Client)
            .Include(diagnosis => diagnosis.Objectives)
            .Include(diagnosis => diagnosis.Assessments)
            .SingleOrDefaultAsync(diagnosis => diagnosis.Id == diagnosisId, cancellationToken);

    private static Dictionary<string, string[]> Validate(DiagnosisInput input)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            errors[nameof(input.Title)] = ["Informe o título do diagnóstico."];
        }

        if (input.AssessmentDate == default)
        {
            errors[nameof(input.AssessmentDate)] = ["Informe a data da avaliação."];
        }

        if (input.Objectives is null)
        {
            errors[nameof(input.Objectives)] = ["Informe a lista de objetivos."];
            return errors;
        }

        if (input.Objectives.Any(objective => objective is null))
        {
            errors[nameof(input.Objectives)] = ["A lista não pode conter objetivos vazios."];
            return errors;
        }

        var objectives = input.Objectives;
        var duplicateCodes = objectives
            .GroupBy(objective => objective.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateCodes.Length > 0)
        {
            errors[nameof(input.Objectives)] = ["Cada objetivo pode ser selecionado apenas uma vez."];
        }

        if (objectives.Count(objective => objective.IsPrimary) > 1)
        {
            errors[nameof(input.Objectives)] = ["Selecione no máximo um objetivo principal."];
        }

        for (var index = 0; index < objectives.Count; index++)
        {
            var objective = objectives[index];
            if (string.IsNullOrWhiteSpace(objective.Code) || !ObjectiveCodes.Contains(objective.Code))
            {
                errors[$"{nameof(input.Objectives)}[{index}].Code"] = ["Selecione um objetivo válido."];
            }
            else if (objective.Code == "Other" && string.IsNullOrWhiteSpace(objective.CustomDescription))
            {
                errors[$"{nameof(input.Objectives)}[{index}].CustomDescription"] =
                    ["Descreva o objetivo selecionado como Outro."];
            }
            else if (objective.Code != "Other" && !string.IsNullOrWhiteSpace(objective.CustomDescription))
            {
                errors[$"{nameof(input.Objectives)}[{index}].CustomDescription"] =
                    ["A descrição personalizada só pode ser informada para o objetivo Outro."];
            }
        }

        return errors;
    }

    private static void AddObjectives(Diagnosis diagnosis, IReadOnlyList<ObjectiveInput> objectives)
    {
        foreach (var objective in objectives)
        {
            diagnosis.Objectives.Add(new DiagnosisObjective
            {
                DiagnosisId = diagnosis.Id,
                Code = objective.Code!,
                IsPrimary = objective.IsPrimary,
                CustomDescription = NormalizeOptional(objective.CustomDescription)
            });
        }
    }

    private static void AddSuggestedPhases(Diagnosis diagnosis, DateTimeOffset now)
    {
        var phaseNames = new[] { "Melhorias rápidas", "MVP", "Evolução" };
        for (var sortOrder = 0; sortOrder < phaseNames.Length; sortOrder++)
        {
            diagnosis.Phases.Add(new Phase
            {
                DiagnosisId = diagnosis.Id,
                Name = phaseNames[sortOrder],
                SortOrder = sortOrder,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
    }

    private static string? ReplaceObjectives(Diagnosis diagnosis, IReadOnlyList<ObjectiveInput> objectives)
    {
        var submittedCodes = objectives.Select(objective => objective.Code!).ToHashSet(StringComparer.Ordinal);
        var existing = diagnosis.Objectives.ToDictionary(objective => objective.Code, StringComparer.Ordinal);

        foreach (var objective in diagnosis.Objectives.ToArray())
        {
            objective.IsPrimary = false;
            if (!submittedCodes.Contains(objective.Code))
            {
                diagnosis.Objectives.Remove(objective);
            }
        }

        foreach (var input in objectives)
        {
            if (existing.TryGetValue(input.Code!, out var objective))
            {
                objective.CustomDescription = NormalizeOptional(input.CustomDescription);
                continue;
            }

            diagnosis.Objectives.Add(new DiagnosisObjective
            {
                DiagnosisId = diagnosis.Id,
                Code = input.Code!,
                IsPrimary = false,
                CustomDescription = NormalizeOptional(input.CustomDescription)
            });
        }

        return objectives.SingleOrDefault(objective => objective.IsPrimary)?.Code;
    }

    private static DiagnosisContract ToContract(Diagnosis diagnosis) => new(
        diagnosis.Id,
        diagnosis.ClientId,
        diagnosis.Client.CompanyName,
        diagnosis.Title,
        diagnosis.AssessmentDate,
        diagnosis.Status.ToString(),
        diagnosis.UpdatedAtUtc,
        diagnosis.Context,
        diagnosis.Objectives
            .OrderBy(objective => objective.Code, StringComparer.Ordinal)
            .Select(objective => new ObjectiveInput(
                objective.Code,
                objective.IsPrimary,
                objective.CustomDescription))
            .ToArray(),
        diagnosis.Assessments
            .OrderBy(assessment => assessment.SectorNameSnapshot, StringComparer.Ordinal)
            .ThenBy(assessment => assessment.Id)
            .Select(ToContract)
            .ToArray());

    private static SectorAssessmentContract ToContract(SectorAssessment assessment) => new(
        assessment.Id,
        assessment.DiagnosisId,
        assessment.ClientSectorId,
        assessment.SectorNameSnapshot,
        assessment.Answer01,
        assessment.Answer02,
        assessment.Answer03,
        assessment.Answer04,
        assessment.Answer05,
        assessment.Answer06,
        assessment.Answer07,
        assessment.Answer08,
        assessment.Answer09,
        assessment.Answer10,
        AnsweredCount(assessment));

    private static int AnsweredCount(SectorAssessment assessment) =>
        new[]
        {
            assessment.Answer01,
            assessment.Answer02,
            assessment.Answer03,
            assessment.Answer04,
            assessment.Answer05,
            assessment.Answer06,
            assessment.Answer07,
            assessment.Answer08,
            assessment.Answer09,
            assessment.Answer10
        }.Count(answer => !string.IsNullOrWhiteSpace(answer));

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
