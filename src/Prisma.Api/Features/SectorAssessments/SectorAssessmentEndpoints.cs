using Microsoft.EntityFrameworkCore;
using Prisma.Api.Data;
using Prisma.Api.Features.Diagnoses;
using Prisma.Contracts.SectorAssessments;

namespace Prisma.Api.Features.SectorAssessments;

public static class SectorAssessmentEndpoints
{
    public static RouteGroupBuilder MapSectorAssessmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/diagnoses/{diagnosisId:guid}/assessments/{assessmentId:guid}")
            .WithTags("SectorAssessments");
        group.MapPut("/answers", ReplaceAnswersAsync).WithName("ReplaceSectorAssessmentAnswers");
        return group;
    }

    private static async Task<IResult> ReplaceAnswersAsync(
        Guid diagnosisId,
        Guid assessmentId,
        AnswersInput input,
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
                title: "Reabra o diagnóstico antes de editar as respostas.");
        }

        var assessment = await dbContext.SectorAssessments
            .SingleOrDefaultAsync(
                candidate => candidate.Id == assessmentId && candidate.DiagnosisId == diagnosisId,
                cancellationToken);
        if (assessment is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Setor avaliado não encontrado.");
        }

        assessment.Answer01 = NormalizeOptional(input.Q01);
        assessment.Answer02 = NormalizeOptional(input.Q02);
        assessment.Answer03 = NormalizeOptional(input.Q03);
        assessment.Answer04 = NormalizeOptional(input.Q04);
        assessment.Answer05 = NormalizeOptional(input.Q05);
        assessment.Answer06 = NormalizeOptional(input.Q06);
        assessment.Answer07 = NormalizeOptional(input.Q07);
        assessment.Answer08 = NormalizeOptional(input.Q08);
        assessment.Answer09 = NormalizeOptional(input.Q09);
        assessment.Answer10 = NormalizeOptional(input.Q10);

        var now = DateTimeOffset.UtcNow;
        assessment.UpdatedAtUtc = now;
        diagnosis.UpdatedAtUtc = now;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível salvar as respostas do setor.");
        }

        return Results.Ok(ToContract(assessment));
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
