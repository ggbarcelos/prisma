using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prisma.Api.Tests.Infrastructure;

namespace Prisma.Api.Tests.Features.Diagnoses;

public sealed class DiagnosisWorkflowTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task MainWorkflow_PersistsDiagnosisGraphAndProjectsSavedReportContent()
    {
        using var client = await CreateApiClientAsync();
        var token = Guid.NewGuid().ToString("N");
        var clientId = await CreateClientAsync(client, token);
        var diagnosisId = await CreateDiagnosisAsync(client, clientId, token);
        var sectorId = await CreateSectorAsync(client, clientId, token);
        var assessmentId = await CreateAssessmentAsync(client, diagnosisId, sectorId);
        await SaveAnswersAsync(client, diagnosisId, assessmentId);
        var bottleneckId = await CreateBottleneckAsync(client, diagnosisId, assessmentId);
        var opportunityId = await CreateOpportunityAsync(client, diagnosisId, bottleneckId);

        using var initialResponse = await client.GetAsync(ReportUrl(diagnosisId));
        Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);
        var initial = await ReadJsonAsync(initialResponse);
        Assert.Equal(diagnosisId, initial.GetProperty("diagnosisId").GetGuid());
        Assert.Equal($"Diagnóstico {token}", initial.GetProperty("diagnosisTitle").GetString());
        Assert.Equal("Diagnóstico ativo", initial.GetProperty("context").GetString());
        Assert.Equal("Draft", initial.GetProperty("status").GetString());
        Assert.Equal($"Empresa {token}", initial.GetProperty("client").GetProperty("companyName").GetString());
        var primaryObjective = Assert.Single(initial.GetProperty("objectives").EnumerateArray());
        Assert.Equal("ReduceCosts", primaryObjective.GetProperty("code").GetString());
        Assert.True(primaryObjective.GetProperty("isPrimary").GetBoolean());
        var assessment = Assert.Single(initial.GetProperty("assessments").EnumerateArray());
        Assert.Equal(1, assessment.GetProperty("answeredCount").GetInt32());
        Assert.Equal("Há conferência manual", assessment.GetProperty("q01").GetString());
        Assert.Equal(bottleneckId, Assert.Single(initial.GetProperty("bottlenecks").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(opportunityId, Assert.Single(initial.GetProperty("unallocatedOpportunities").EnumerateArray()).GetProperty("id").GetGuid());
        var phases = initial.GetProperty("phases").EnumerateArray().ToArray();
        Assert.Equal(3, phases.Length);
        Assert.Equal(
            new[] { "Melhorias rápidas", "MVP", "Evolução" },
            phases.Select(phase => phase.GetProperty("name").GetString()).ToArray());

        var opportunity = Assert.Single(initial.GetProperty("opportunities").EnumerateArray());
        Assert.Equal(opportunityId, opportunity.GetProperty("id").GetGuid());
        Assert.Equal(9, opportunity.GetProperty("priorityScore").GetInt32());
        Assert.Equal(bottleneckId, Assert.Single(opportunity.GetProperty("bottleneckIds").EnumerateArray()).GetGuid());
        Assert.Equal(4, opportunity.GetProperty("impact").GetInt32());

        using var updateResponse = await client.PutAsJsonAsync(
            ReportUrl(diagnosisId),
            new
            {
                executiveSummary = "  Resumo executivo  ",
                generalRecommendations = "Recomendações gerais",
                nextSteps = "   "
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var saved = await ReadJsonAsync(updateResponse);
        Assert.Equal("Resumo executivo", saved.GetProperty("executiveSummary").GetString());
        Assert.Equal("Recomendações gerais", saved.GetProperty("generalRecommendations").GetString());
        Assert.Equal(JsonValueKind.Null, saved.GetProperty("nextSteps").ValueKind);

        using var reloadResponse = await client.GetAsync(ReportUrl(diagnosisId));
        Assert.Equal(HttpStatusCode.OK, reloadResponse.StatusCode);
        var persisted = await ReadJsonAsync(reloadResponse);
        Assert.Equal("Resumo executivo", persisted.GetProperty("executiveSummary").GetString());
        Assert.Equal("Recomendações gerais", persisted.GetProperty("generalRecommendations").GetString());
        Assert.Equal(JsonValueKind.Null, persisted.GetProperty("nextSteps").ValueKind);
    }

    private async Task<HttpClient> CreateApiClientAsync()
    {
        await factory.ApplyMigrationsAsync();
        return factory.CreateClient();
    }

    private static async Task<Guid> CreateClientAsync(HttpClient client, string token)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/clients",
            new
            {
                companyName = $"Empresa {token}",
                segment = "Serviços",
                contactName = (string?)null,
                email = (string?)null,
                phone = (string?)null,
                notes = (string?)null
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateDiagnosisAsync(HttpClient client, Guid clientId, string token)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/diagnoses",
            new
            {
                clientId,
                title = $"Diagnóstico {token}",
                assessmentDate = DateOnly.FromDateTime(DateTime.Today),
                context = "Diagnóstico ativo",
                objectives = new[]
                {
                    new { code = "ReduceCosts", isPrimary = true, customDescription = (string?)null }
                }
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateSectorAsync(HttpClient client, Guid clientId, string token)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/clients/{clientId}/sectors",
            new { name = $"Operação {token}", responsibleName = (string?)null, peopleCount = (int?)null },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateAssessmentAsync(HttpClient client, Guid diagnosisId, Guid sectorId)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/diagnoses/{diagnosisId}/assessments",
            new { clientSectorId = sectorId },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task SaveAnswersAsync(HttpClient client, Guid diagnosisId, Guid assessmentId)
    {
        using var response = await client.PutAsJsonAsync(
            $"/api/diagnoses/{diagnosisId}/assessments/{assessmentId}/answers",
            new
            {
                q01 = "Há conferência manual",
                q02 = (string?)null,
                q03 = (string?)null,
                q04 = (string?)null,
                q05 = (string?)null,
                q06 = (string?)null,
                q07 = (string?)null,
                q08 = (string?)null,
                q09 = (string?)null,
                q10 = (string?)null
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<Guid> CreateBottleneckAsync(HttpClient client, Guid diagnosisId, Guid assessmentId)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/diagnoses/{diagnosisId}/bottlenecks",
            new
            {
                sectorAssessmentId = assessmentId,
                title = "Conferência manual",
                description = "A equipe confere os dados manualmente.",
                category = "ManualWork",
                impact = 4,
                frequency = "Diária",
                evidence = "Amostra recente",
                probableCause = "Sistemas não integrados",
                estimatedMonthlyHours = 8m,
                notes = (string?)null
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateOpportunityAsync(HttpClient client, Guid diagnosisId, Guid bottleneckId)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/diagnoses/{diagnosisId}/opportunities",
            new
            {
                title = "Automatizar conferência",
                solutionDescription = "Consolidar os dados em uma etapa automática.",
                type = "Automation",
                expectedBenefit = "Reduzir retrabalho",
                impact = 4,
                effort = 2,
                urgency = 3,
                dependencies = (string?)null,
                aiDataNeeds = (string?)null,
                needsHumanReview = (bool?)null,
                phaseId = (Guid?)null,
                bottleneckIds = new[] { bottleneckId }
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static string ReportUrl(Guid diagnosisId) => $"/api/diagnoses/{diagnosisId}/report";

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
