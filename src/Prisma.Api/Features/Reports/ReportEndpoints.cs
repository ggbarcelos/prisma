using Microsoft.EntityFrameworkCore;
using Prisma.Api.Data;
using Prisma.Api.Features.Bottlenecks;
using Prisma.Api.Features.Diagnoses;
using Prisma.Api.Features.Opportunities;
using Prisma.Api.Features.Phases;
using Prisma.Api.Features.SectorAssessments;
using Prisma.Contracts.Bottlenecks;
using Prisma.Contracts.Clients;
using Prisma.Contracts.Opportunities;
using Prisma.Contracts.Phases;
using Prisma.Contracts.Reports;
using Prisma.Contracts.SectorAssessments;

namespace Prisma.Api.Features.Reports;

public static class ReportEndpoints
{
    public static RouteGroupBuilder MapReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/diagnoses/{diagnosisId:guid}/report")
            .WithTags("Reports");
        group.MapGet("", GetAsync).WithName("GetDiagnosisReport");
        group.MapPut("", UpdateAsync).WithName("UpdateDiagnosisReport");
        return group;
    }

    private static async Task<IResult> GetAsync(
        Guid diagnosisId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var report = await BuildReportAsync(dbContext, diagnosisId, cancellationToken);
        return report is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Diagnóstico não encontrado.")
            : Results.Ok(report);
    }

    private static async Task<IResult> UpdateAsync(
        Guid diagnosisId,
        DiagnosisReportInput input,
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
                title: "Reabra o diagnóstico antes de editar o relatório.");
        }

        diagnosis.ExecutiveSummary = NormalizeOptional(input.ExecutiveSummary);
        diagnosis.GeneralRecommendations = NormalizeOptional(input.GeneralRecommendations);
        diagnosis.NextSteps = NormalizeOptional(input.NextSteps);
        diagnosis.UpdatedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Não foi possível salvar os textos do relatório.");
        }

        var report = await BuildReportAsync(dbContext, diagnosisId, cancellationToken);
        return report is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Diagnóstico não encontrado.")
            : Results.Ok(report);
    }

    private static async Task<DiagnosisReportContract?> BuildReportAsync(
        AppDbContext dbContext,
        Guid diagnosisId,
        CancellationToken cancellationToken)
    {
        var diagnosis = await dbContext.Diagnoses
            .AsNoTracking()
            .Include(candidate => candidate.Client)
            .Include(candidate => candidate.Objectives)
            .SingleOrDefaultAsync(candidate => candidate.Id == diagnosisId, cancellationToken);
        if (diagnosis is null)
        {
            return null;
        }

        var assessments = await dbContext.SectorAssessments
            .AsNoTracking()
            .Where(assessment => assessment.DiagnosisId == diagnosisId)
            .OrderBy(assessment => assessment.SectorNameSnapshot)
            .ThenBy(assessment => assessment.Id)
            .ToListAsync(cancellationToken);
        var bottlenecks = await dbContext.Bottlenecks
            .AsNoTracking()
            .Where(bottleneck => bottleneck.DiagnosisId == diagnosisId)
            .OrderByDescending(bottleneck => bottleneck.CreatedAtUtc)
            .ThenBy(bottleneck => bottleneck.Title)
            .ThenBy(bottleneck => bottleneck.Id)
            .ToListAsync(cancellationToken);
        var opportunityEntities = await dbContext.Opportunities
            .AsNoTracking()
            .Include(opportunity => opportunity.BottleneckLinks)
            .Where(opportunity => opportunity.DiagnosisId == diagnosisId)
            .ToListAsync(cancellationToken);
        var opportunities = opportunityEntities
            .OrderByDescending(PriorityScore)
            .ThenByDescending(opportunity => opportunity.Impact)
            .ThenBy(opportunity => opportunity.Effort)
            .ThenBy(opportunity => opportunity.Title, StringComparer.Ordinal)
            .ThenBy(opportunity => opportunity.Id)
            .Select(ToContract)
            .ToArray();
        var phases = await dbContext.Phases
            .AsNoTracking()
            .Where(phase => phase.DiagnosisId == diagnosisId)
            .OrderBy(phase => phase.SortOrder)
            .ThenBy(phase => phase.Id)
            .ToListAsync(cancellationToken);

        var client = diagnosis.Client;
        return new DiagnosisReportContract(
            diagnosis.Id,
            diagnosis.Title,
            diagnosis.AssessmentDate,
            diagnosis.Context,
            diagnosis.Status.ToString(),
            new ClientContract(
                client.Id,
                client.CompanyName,
                client.Segment,
                client.ContactName,
                client.Email,
                client.Phone,
                client.Notes,
                client.CreatedAtUtc,
                client.UpdatedAtUtc),
            diagnosis.Objectives
                .OrderBy(objective => objective.Code, StringComparer.Ordinal)
                .Select(objective => new DiagnosisReportObjectiveContract(
                    objective.Code,
                    objective.IsPrimary,
                    objective.CustomDescription))
                .ToArray(),
            diagnosis.ExecutiveSummary,
            diagnosis.GeneralRecommendations,
            assessments.Select(ToContract).ToArray(),
            bottlenecks.Select(ToContract).ToArray(),
            opportunities,
            phases.Select(ToContract).ToArray(),
            opportunities.Where(opportunity => opportunity.PhaseId is null).ToArray(),
            diagnosis.NextSteps);
    }

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

    private static PhaseContract ToContract(Phase phase) => new(
        phase.Id,
        phase.DiagnosisId,
        phase.Name,
        phase.Description,
        phase.SortOrder);

    private static int PriorityScore(Opportunity opportunity) =>
        (opportunity.Impact * 2) + opportunity.Urgency - opportunity.Effort;

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
