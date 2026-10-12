using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prisma.Api.Tests.Infrastructure;

namespace Prisma.Api.Tests.Features.Opportunities;

public sealed class OpportunityTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateOpportunity_CalculatesFormulaAndKeepsDocumentedScoreLimits()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithBottleneckAsync(client);

        var minimum = await CreateOpportunityAsync(
            client, setup.DiagnosisId, [setup.BottleneckId], "Pontuação mínima", impact: 1, effort: 5, urgency: 1);
        var formulaExample = await CreateOpportunityAsync(
            client, setup.DiagnosisId, [setup.BottleneckId], "Exemplo de fórmula", impact: 4, effort: 2, urgency: 3);
        var maximum = await CreateOpportunityAsync(
            client, setup.DiagnosisId, [setup.BottleneckId], "Pontuação máxima", impact: 5, effort: 1, urgency: 5);

        Assert.Equal(-2, minimum.PriorityScore);
        Assert.Equal(9, formulaExample.PriorityScore);
        Assert.Equal(14, maximum.PriorityScore);
    }

    [Fact]
    public async Task ListOpportunities_OrdersByScoreAndDocumentedTieBreakers()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithBottleneckAsync(client);

        await CreateOpportunityAsync(client, setup.DiagnosisId, [setup.BottleneckId], "Pontuação menor", 1, 4, 5);
        await CreateOpportunityAsync(client, setup.DiagnosisId, [setup.BottleneckId], "Alfa", 3, 2, 3);
        await CreateOpportunityAsync(client, setup.DiagnosisId, [setup.BottleneckId], "Zulu", 3, 2, 3);
        await CreateOpportunityAsync(client, setup.DiagnosisId, [setup.BottleneckId], "Esforço menor", 3, 1, 2);
        await CreateOpportunityAsync(client, setup.DiagnosisId, [setup.BottleneckId], "Impacto maior", 4, 3, 2);
        await CreateOpportunityAsync(client, setup.DiagnosisId, [setup.BottleneckId], "Pontuação maior", 5, 1, 5);

        using var response = await client.GetAsync(OpportunitiesUrl(setup.DiagnosisId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await ReadJsonAsync(response);

        Assert.Equal(
            new[] { "Pontuação maior", "Impacto maior", "Esforço menor", "Alfa", "Zulu", "Pontuação menor" },
            items.EnumerateArray().Select(item => item.GetProperty("title").GetString()));
        Assert.Equal(
            new[] { 14, 7, 7, 7, 7, 3 },
            items.EnumerateArray().Select(item => item.GetProperty("priorityScore").GetInt32()));
    }

    [Fact]
    public async Task CreateOpportunity_RejectsInvalidRequiredValuesAndScoreInputs()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithBottleneckAsync(client);

        using var missingTitle = await client.PostAsJsonAsync(
            OpportunitiesUrl(setup.DiagnosisId), OpportunityInput([setup.BottleneckId], title: "   "), JsonOptions);
        using var missingType = await client.PostAsJsonAsync(
            OpportunitiesUrl(setup.DiagnosisId), OpportunityInput([setup.BottleneckId], type: "UnknownType"), JsonOptions);
        using var missingBenefit = await client.PostAsJsonAsync(
            OpportunitiesUrl(setup.DiagnosisId), OpportunityInput([setup.BottleneckId], expectedBenefit: "  "), JsonOptions);
        using var invalidImpact = await client.PostAsJsonAsync(
            OpportunitiesUrl(setup.DiagnosisId), OpportunityInput([setup.BottleneckId], impact: 0), JsonOptions);
        using var invalidEffort = await client.PostAsJsonAsync(
            OpportunitiesUrl(setup.DiagnosisId), OpportunityInput([setup.BottleneckId], effort: 6), JsonOptions);
        using var invalidUrgency = await client.PostAsJsonAsync(
            OpportunitiesUrl(setup.DiagnosisId), OpportunityInput([setup.BottleneckId], urgency: 0), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, missingTitle.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingType.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingBenefit.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidImpact.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidEffort.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidUrgency.StatusCode);
    }

    [Fact]
    public async Task CreateOpportunity_RequiresNonemptyUniqueBottlenecksFromSameDiagnosis()
    {
        using var client = await CreateApiClientAsync();
        var first = await CreateDiagnosisWithBottleneckAsync(client);
        var second = await CreateDiagnosisWithBottleneckAsync(client);

        using var emptyLinks = await client.PostAsJsonAsync(
            OpportunitiesUrl(first.DiagnosisId), OpportunityInput([]), JsonOptions);
        using var duplicateLinks = await client.PostAsJsonAsync(
            OpportunitiesUrl(first.DiagnosisId), OpportunityInput([first.BottleneckId, first.BottleneckId]), JsonOptions);
        using var crossDiagnosisLinks = await client.PostAsJsonAsync(
            OpportunitiesUrl(first.DiagnosisId), OpportunityInput([second.BottleneckId]), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, emptyLinks.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateLinks.StatusCode);
        AssertOwnedResourceConflictOrNotFound(crossDiagnosisLinks.StatusCode);
    }

    [Fact]
    public async Task Opportunity_CanBeUpdatedAndDeletedWithItsBottleneckLinks()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithBottleneckAsync(client);
        var additionalBottleneckId = await CreateBottleneckAsync(
            client, setup.DiagnosisId, setup.AssessmentId, "Aprovações manuais");
        var created = await CreateOpportunityAsync(
            client, setup.DiagnosisId, [setup.BottleneckId], "Automatizar conferência", 4, 2, 3);

        using var updateResponse = await client.PutAsJsonAsync(
            $"{OpportunitiesUrl(setup.DiagnosisId)}/{created.Id}",
            new
            {
                title = "  Apoiar aprovações  ",
                solutionDescription = (string?)null,
                type = "AiApplication",
                expectedBenefit = "Reduzir o tempo de aprovação",
                impact = 5,
                effort = 1,
                urgency = 5,
                dependencies = (string?)null,
                aiDataNeeds = "Histórico interno de aprovações",
                needsHumanReview = true,
                phaseId = (Guid?)null,
                bottleneckIds = new[] { additionalBottleneckId }
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await ReadJsonAsync(updateResponse);
        Assert.Equal("Apoiar aprovações", updated.GetProperty("title").GetString());
        Assert.Equal("AiApplication", updated.GetProperty("type").GetString());
        Assert.Equal(14, updated.GetProperty("priorityScore").GetInt32());
        Assert.Equal("Histórico interno de aprovações", updated.GetProperty("aiDataNeeds").GetString());
        Assert.True(updated.GetProperty("needsHumanReview").GetBoolean());
        Assert.Equal(additionalBottleneckId, Assert.Single(updated.GetProperty("bottleneckIds").EnumerateArray()).GetGuid());

        using var deleteResponse = await client.DeleteAsync($"{OpportunitiesUrl(setup.DiagnosisId)}/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var listResponse = await client.GetAsync(OpportunitiesUrl(setup.DiagnosisId));
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Empty((await ReadJsonAsync(listResponse)).EnumerateArray());

        using var originalBottleneckDelete = await client.DeleteAsync(
            $"/api/diagnoses/{setup.DiagnosisId}/bottlenecks/{setup.BottleneckId}");
        using var reassignedBottleneckDelete = await client.DeleteAsync(
            $"/api/diagnoses/{setup.DiagnosisId}/bottlenecks/{additionalBottleneckId}");
        Assert.Equal(HttpStatusCode.NoContent, originalBottleneckDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, reassignedBottleneckDelete.StatusCode);
    }

    private async Task<HttpClient> CreateApiClientAsync()
    {
        await factory.ApplyMigrationsAsync();
        return factory.CreateClient();
    }

    private static async Task<(Guid DiagnosisId, Guid AssessmentId, Guid BottleneckId)> CreateDiagnosisWithBottleneckAsync(HttpClient client)
    {
        var token = Guid.NewGuid().ToString("N");
        using var clientResponse = await client.PostAsJsonAsync(
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
        Assert.Equal(HttpStatusCode.Created, clientResponse.StatusCode);
        var createdClient = await ReadJsonAsync(clientResponse);
        var clientId = createdClient.GetProperty("id").GetGuid();

        using var diagnosisResponse = await client.PostAsJsonAsync(
            "/api/diagnoses",
            new
            {
                clientId,
                title = $"Diagnóstico {token}",
                assessmentDate = DateOnly.FromDateTime(DateTime.Today),
                context = (string?)null,
                objectives = new[]
                {
                    new { code = "ReduceCosts", isPrimary = true, customDescription = (string?)null }
                }
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, diagnosisResponse.StatusCode);
        var createdDiagnosis = await ReadJsonAsync(diagnosisResponse);
        var diagnosisId = createdDiagnosis.GetProperty("id").GetGuid();

        using var sectorResponse = await client.PostAsJsonAsync(
            $"/api/clients/{clientId}/sectors",
            new { name = "Operação", responsibleName = (string?)null, peopleCount = (int?)null },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, sectorResponse.StatusCode);
        var sector = await ReadJsonAsync(sectorResponse);
        var sectorId = sector.GetProperty("id").GetGuid();

        using var assessmentResponse = await client.PostAsJsonAsync(
            $"/api/diagnoses/{diagnosisId}/assessments",
            new { clientSectorId = sectorId },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, assessmentResponse.StatusCode);
        var assessment = await ReadJsonAsync(assessmentResponse);
        var assessmentId = assessment.GetProperty("id").GetGuid();

        using var bottleneckResponse = await client.PostAsJsonAsync(
            $"/api/diagnoses/{diagnosisId}/bottlenecks",
            new
            {
                sectorAssessmentId = assessmentId,
                title = "Conferência manual",
                description = "A equipe confere as informações manualmente.",
                category = "ManualWork",
                impact = 3,
                frequency = "Diária",
                evidence = "Amostra de conferências",
                probableCause = "Sistemas separados",
                estimatedMonthlyHours = (decimal?)null,
                notes = (string?)null
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, bottleneckResponse.StatusCode);
        var bottleneck = await ReadJsonAsync(bottleneckResponse);
        return (diagnosisId, assessmentId, bottleneck.GetProperty("id").GetGuid());
    }

    private static async Task<Guid> CreateBottleneckAsync(HttpClient client, Guid diagnosisId, Guid assessmentId, string title)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/diagnoses/{diagnosisId}/bottlenecks",
            new
            {
                sectorAssessmentId = assessmentId,
                title,
                description = (string?)null,
                category = "ManualWork",
                impact = 3,
                frequency = (string?)null,
                evidence = (string?)null,
                probableCause = (string?)null,
                estimatedMonthlyHours = (decimal?)null,
                notes = (string?)null
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var bottleneck = await ReadJsonAsync(response);
        return bottleneck.GetProperty("id").GetGuid();
    }

    private static async Task<CreatedOpportunity> CreateOpportunityAsync(
        HttpClient client,
        Guid diagnosisId,
        IReadOnlyList<Guid> bottleneckIds,
        string title,
        int impact,
        int effort,
        int urgency)
    {
        using var response = await client.PostAsJsonAsync(
            OpportunitiesUrl(diagnosisId),
            OpportunityInput(bottleneckIds, title, impact: impact, effort: effort, urgency: urgency),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        return new CreatedOpportunity(
            created.GetProperty("id").GetGuid(),
            created.GetProperty("title").GetString()!,
            created.GetProperty("priorityScore").GetInt32());
    }

    private static object OpportunityInput(
        IReadOnlyList<Guid> bottleneckIds,
        string? title = "Automatizar conferência",
        string? type = "Automation",
        string? expectedBenefit = "Reduzir retrabalho manual",
        int impact = 4,
        int effort = 2,
        int urgency = 3) => new
    {
        title,
        solutionDescription = "Integrar os sistemas de origem.",
        type,
        expectedBenefit,
        impact,
        effort,
        urgency,
        dependencies = (string?)null,
        aiDataNeeds = (string?)null,
        needsHumanReview = (bool?)null,
        phaseId = (Guid?)null,
        bottleneckIds
    };

    private static string OpportunitiesUrl(Guid diagnosisId) => $"/api/diagnoses/{diagnosisId}/opportunities";

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private static void AssertOwnedResourceConflictOrNotFound(HttpStatusCode statusCode) =>
        Assert.Contains(statusCode, new[] { HttpStatusCode.Conflict, HttpStatusCode.NotFound });

    private sealed record CreatedOpportunity(Guid Id, string Title, int PriorityScore);
}
