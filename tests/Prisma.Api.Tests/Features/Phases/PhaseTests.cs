using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prisma.Api.Tests.Infrastructure;

namespace Prisma.Api.Tests.Features.Phases;

public sealed class PhaseTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateDiagnosis_CreatesThreeSuggestedPhasesInOrder()
    {
        using var client = await CreateApiClientAsync();
        var diagnosisId = await CreateDiagnosisAsync(client);

        using var response = await client.GetAsync(PhasesUrl(diagnosisId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var phases = await ReadJsonAsync(response);
        var items = phases.EnumerateArray().ToArray();
        Assert.Equal(
            new[] { "Melhorias rápidas", "MVP", "Evolução" },
            items.Select(item => item.GetProperty("name").GetString()));
        Assert.Equal(
            items.Select(item => item.GetProperty("sortOrder").GetInt32()).OrderBy(value => value),
            items.Select(item => item.GetProperty("sortOrder").GetInt32()));
        Assert.Equal(3, items.Select(item => item.GetProperty("id").GetGuid()).Distinct().Count());
        Assert.All(items, item => Assert.Equal(diagnosisId, item.GetProperty("diagnosisId").GetGuid()));
    }

    [Fact]
    public async Task CreatePhase_AppendsAnEditablePhaseAfterSuggestedPhases()
    {
        using var client = await CreateApiClientAsync();
        var diagnosisId = await CreateDiagnosisAsync(client);
        var initialPhases = await GetPhasesAsync(client, diagnosisId);

        using var createResponse = await client.PostAsJsonAsync(
            PhasesUrl(diagnosisId),
            new { name = "Escala", description = "Expandir a solução após validação." },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await ReadJsonAsync(createResponse);
        Assert.Equal(diagnosisId, created.GetProperty("diagnosisId").GetGuid());
        Assert.Equal("Escala", created.GetProperty("name").GetString());
        Assert.Equal(initialPhases.Max(phase => phase.GetProperty("sortOrder").GetInt32()) + 1,
            created.GetProperty("sortOrder").GetInt32());

        var persisted = await GetPhasesAsync(client, diagnosisId);
        Assert.Equal(initialPhases.Length + 1, persisted.Length);
        Assert.Equal("Escala", persisted[^1].GetProperty("name").GetString());
    }

    [Fact]
    public async Task UpdatePhase_PersistsNameAndDescription()
    {
        using var client = await CreateApiClientAsync();
        var diagnosisId = await CreateDiagnosisAsync(client);
        var phases = await GetPhasesAsync(client, diagnosisId);
        var phase = phases[1];
        var phaseId = phase.GetProperty("id").GetGuid();
        var originalOrder = phase.GetProperty("sortOrder").GetInt32();

        using var updateResponse = await client.PutAsJsonAsync(
            $"{PhasesUrl(diagnosisId)}/{phaseId}",
            new { name = "Entrega inicial", description = "Validar a proposta com a equipe." },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await ReadJsonAsync(updateResponse);
        Assert.Equal(phaseId, updated.GetProperty("id").GetGuid());
        Assert.Equal("Entrega inicial", updated.GetProperty("name").GetString());
        Assert.Equal("Validar a proposta com a equipe.", updated.GetProperty("description").GetString());
        Assert.Equal(originalOrder, updated.GetProperty("sortOrder").GetInt32());

        var persisted = await GetPhasesAsync(client, diagnosisId);
        Assert.Equal("Entrega inicial", persisted[1].GetProperty("name").GetString());
        Assert.Equal("Validar a proposta com a equipe.", persisted[1].GetProperty("description").GetString());
    }

    [Fact]
    public async Task ReorderPhases_PersistsCompleteOrderAndRejectsIncompleteOrder()
    {
        using var client = await CreateApiClientAsync();
        var diagnosisId = await CreateDiagnosisAsync(client);
        var phases = await GetPhasesAsync(client, diagnosisId);
        var initialIds = phases.Select(phase => phase.GetProperty("id").GetGuid()).ToArray();
        var reorderedIds = initialIds.Reverse().ToArray();

        using var reorderResponse = await client.PutAsJsonAsync(
            $"{PhasesUrl(diagnosisId)}/order",
            new { phaseIds = reorderedIds },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, reorderResponse.StatusCode);
        var reordered = await ReadJsonAsync(reorderResponse);
        Assert.Equal(reorderedIds, reordered.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()));
        var sortOrders = reordered.EnumerateArray().Select(item => item.GetProperty("sortOrder").GetInt32()).ToArray();
        Assert.Equal(sortOrders.OrderBy(value => value), sortOrders);
        Assert.Equal(sortOrders.Length, sortOrders.Distinct().Count());

        using var incompleteOrderResponse = await client.PutAsJsonAsync(
            $"{PhasesUrl(diagnosisId)}/order",
            new { phaseIds = reorderedIds[..^1] },
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, incompleteOrderResponse.StatusCode);
        var persisted = await GetPhasesAsync(client, diagnosisId);
        Assert.Equal(reorderedIds, persisted.Select(item => item.GetProperty("id").GetGuid()));
    }

    [Fact]
    public async Task OpportunityPhase_IsOptionalAndRejectsPhaseFromAnotherDiagnosis()
    {
        using var client = await CreateApiClientAsync();
        var first = await CreateDiagnosisWithBottleneckAsync(client);
        var second = await CreateDiagnosisWithBottleneckAsync(client);
        var firstPhase = (await GetPhasesAsync(client, first.DiagnosisId))[0];
        var secondPhase = (await GetPhasesAsync(client, second.DiagnosisId))[0];
        var firstPhaseId = firstPhase.GetProperty("id").GetGuid();
        var secondPhaseId = secondPhase.GetProperty("id").GetGuid();

        using var createResponse = await client.PostAsJsonAsync(
            OpportunitiesUrl(first.DiagnosisId),
            OpportunityInput(first.BottleneckId, phaseId: null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await ReadJsonAsync(createResponse);
        var opportunityId = created.GetProperty("id").GetGuid();
        Assert.Equal(JsonValueKind.Null, created.GetProperty("phaseId").ValueKind);

        using var assignResponse = await client.PutAsJsonAsync(
            $"{OpportunitiesUrl(first.DiagnosisId)}/{opportunityId}",
            OpportunityInput(first.BottleneckId, phaseId: firstPhaseId),
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        var assigned = await ReadJsonAsync(assignResponse);
        Assert.Equal(firstPhaseId, assigned.GetProperty("phaseId").GetGuid());

        using var crossDiagnosisResponse = await client.PutAsJsonAsync(
            $"{OpportunitiesUrl(first.DiagnosisId)}/{opportunityId}",
            OpportunityInput(first.BottleneckId, phaseId: secondPhaseId),
            JsonOptions);

        Assert.Contains(crossDiagnosisResponse.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Conflict });
        var persisted = await GetOpportunityAsync(client, first.DiagnosisId, opportunityId);
        Assert.Equal(firstPhaseId, persisted.GetProperty("phaseId").GetGuid());
    }

    private async Task<HttpClient> CreateApiClientAsync()
    {
        await factory.ApplyMigrationsAsync();
        return factory.CreateClient();
    }

    private static async Task<Guid> CreateDiagnosisAsync(HttpClient client)
    {
        var token = NewToken();
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

        using var diagnosisResponse = await client.PostAsJsonAsync(
            "/api/diagnoses",
            new
            {
                clientId = createdClient.GetProperty("id").GetGuid(),
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
        var diagnosis = await ReadJsonAsync(diagnosisResponse);
        return diagnosis.GetProperty("id").GetGuid();
    }

    private static async Task<(Guid DiagnosisId, Guid BottleneckId)> CreateDiagnosisWithBottleneckAsync(HttpClient client)
    {
        var diagnosisId = await CreateDiagnosisAsync(client);
        using var diagnosisResponse = await client.GetAsync($"/api/diagnoses/{diagnosisId}");
        Assert.Equal(HttpStatusCode.OK, diagnosisResponse.StatusCode);
        var diagnosis = await ReadJsonAsync(diagnosisResponse);
        var clientId = diagnosis.GetProperty("clientId").GetGuid();
        var token = NewToken();

        using var sectorResponse = await client.PostAsJsonAsync(
            $"/api/clients/{clientId}/sectors",
            new { name = $"Operação {token}", responsibleName = (string?)null, peopleCount = (int?)null },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, sectorResponse.StatusCode);
        var sector = await ReadJsonAsync(sectorResponse);

        using var assessmentResponse = await client.PostAsJsonAsync(
            $"/api/diagnoses/{diagnosisId}/assessments",
            new { clientSectorId = sector.GetProperty("id").GetGuid() },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, assessmentResponse.StatusCode);
        var assessment = await ReadJsonAsync(assessmentResponse);

        using var bottleneckResponse = await client.PostAsJsonAsync(
            $"/api/diagnoses/{diagnosisId}/bottlenecks",
            new
            {
                sectorAssessmentId = assessment.GetProperty("id").GetGuid(),
                title = "Conferência manual",
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
        Assert.Equal(HttpStatusCode.Created, bottleneckResponse.StatusCode);
        var bottleneck = await ReadJsonAsync(bottleneckResponse);
        return (diagnosisId, bottleneck.GetProperty("id").GetGuid());
    }

    private static async Task<JsonElement[]> GetPhasesAsync(HttpClient client, Guid diagnosisId)
    {
        using var response = await client.GetAsync(PhasesUrl(diagnosisId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var phases = await ReadJsonAsync(response);
        return phases.EnumerateArray().ToArray();
    }

    private static async Task<JsonElement> GetOpportunityAsync(HttpClient client, Guid diagnosisId, Guid opportunityId)
    {
        using var response = await client.GetAsync(OpportunitiesUrl(diagnosisId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var opportunities = await ReadJsonAsync(response);
        return Assert.Single(
            opportunities.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == opportunityId);
    }

    private static object OpportunityInput(Guid bottleneckId, Guid? phaseId) => new
    {
        title = "Automatizar conferência",
        solutionDescription = (string?)null,
        type = "Automation",
        expectedBenefit = "Reduzir retrabalho manual",
        impact = 4,
        effort = 2,
        urgency = 3,
        dependencies = (string?)null,
        aiDataNeeds = (string?)null,
        needsHumanReview = (bool?)null,
        phaseId,
        bottleneckIds = new[] { bottleneckId }
    };

    private static string PhasesUrl(Guid diagnosisId) => $"/api/diagnoses/{diagnosisId}/phases";

    private static string OpportunitiesUrl(Guid diagnosisId) => $"/api/diagnoses/{diagnosisId}/opportunities";

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private static string NewToken() => Guid.NewGuid().ToString("N");
}
