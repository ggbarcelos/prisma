using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prisma.Api.Tests.Infrastructure;

namespace Prisma.Api.Tests.Features.Diagnoses;

public sealed class DiagnosisStatusTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CompleteDiagnosis_RequiresPrimaryBottleneckAndOpportunityButNotPhase()
    {
        using var client = await CreateApiClientAsync();

        var noPrimary = await CreateScenarioAsync(client, includePrimaryObjective: false);
        var noPrimaryBottleneckId = await CreateBottleneckAsync(client, noPrimary);
        await CreateOpportunityAsync(client, noPrimary, noPrimaryBottleneckId);

        using var missingPrimaryResponse = await SetStatusAsync(client, noPrimary.DiagnosisId, "Completed");

        Assert.Equal(HttpStatusCode.Conflict, missingPrimaryResponse.StatusCode);
        Assert.Contains("objetivo principal", await missingPrimaryResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Draft", await ReadDiagnosisStatusAsync(client, noPrimary.DiagnosisId));

        var scenario = await CreateScenarioAsync(client);
        using var missingItemsResponse = await SetStatusAsync(client, scenario.DiagnosisId, "Completed");
        var missingItemsBody = await missingItemsResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, missingItemsResponse.StatusCode);
        Assert.Contains("gargalo", missingItemsBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("oportunidade", missingItemsBody, StringComparison.OrdinalIgnoreCase);

        var bottleneckId = await CreateBottleneckAsync(client, scenario);
        using var missingOpportunityResponse = await SetStatusAsync(client, scenario.DiagnosisId, "Completed");
        Assert.Equal(HttpStatusCode.Conflict, missingOpportunityResponse.StatusCode);
        Assert.Contains("oportunidade", await missingOpportunityResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var opportunityId = await CreateOpportunityAsync(client, scenario, bottleneckId);
        using var completedResponse = await SetStatusAsync(client, scenario.DiagnosisId, "Completed");

        Assert.Equal(HttpStatusCode.OK, completedResponse.StatusCode);
        Assert.Equal("Completed", (await ReadJsonAsync(completedResponse)).GetProperty("status").GetString());
        using var opportunityListResponse = await client.GetAsync(OpportunitiesUrl(scenario.DiagnosisId));
        Assert.Equal(HttpStatusCode.OK, opportunityListResponse.StatusCode);
        var persistedOpportunity = Assert.Single(
            (await ReadJsonAsync(opportunityListResponse)).EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == opportunityId);
        Assert.Equal(JsonValueKind.Null, persistedOpportunity.GetProperty("phaseId").ValueKind);
    }

    [Fact]
    public async Task DiagnosisStatus_CanToggleAndReopenWhilePreservingPersistedContent()
    {
        using var client = await CreateApiClientAsync();
        var scenario = await CreateScenarioAsync(client);
        var bottleneckId = await CreateBottleneckAsync(client, scenario);
        var opportunityId = await CreateOpportunityAsync(client, scenario, bottleneckId);
        const string savedAnswer = "A equipe registra as conferências no sistema existente.";
        await SaveAnswersAsync(client, scenario.DiagnosisId, scenario.AssessmentId, savedAnswer);

        using var inProgress = await SetStatusAsync(client, scenario.DiagnosisId, "InProgress");
        Assert.Equal(HttpStatusCode.OK, inProgress.StatusCode);
        Assert.Equal("InProgress", (await ReadJsonAsync(inProgress)).GetProperty("status").GetString());

        using var draft = await SetStatusAsync(client, scenario.DiagnosisId, "Draft");
        Assert.Equal(HttpStatusCode.OK, draft.StatusCode);
        Assert.Equal("Draft", (await ReadJsonAsync(draft)).GetProperty("status").GetString());

        using var activeAgain = await SetStatusAsync(client, scenario.DiagnosisId, "InProgress");
        Assert.Equal(HttpStatusCode.OK, activeAgain.StatusCode);
        Assert.Equal("InProgress", (await ReadJsonAsync(activeAgain)).GetProperty("status").GetString());
        using var completed = await SetStatusAsync(client, scenario.DiagnosisId, "Completed");
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal("Completed", (await ReadJsonAsync(completed)).GetProperty("status").GetString());

        using var forbiddenStatusChange = await SetStatusAsync(client, scenario.DiagnosisId, "Draft");
        Assert.Equal(HttpStatusCode.Conflict, forbiddenStatusChange.StatusCode);

        using var forbiddenDiagnosisEdit = await client.PutAsJsonAsync(
            DiagnosisUrl(scenario.DiagnosisId),
            DiagnosisInput(scenario.ClientId, "Título não persistido", "Contexto não persistido"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, forbiddenDiagnosisEdit.StatusCode);

        using var reopen = await SetStatusAsync(client, scenario.DiagnosisId, "InProgress");
        Assert.Equal(HttpStatusCode.OK, reopen.StatusCode);
        Assert.Equal("InProgress", (await ReadJsonAsync(reopen)).GetProperty("status").GetString());

        using var preservedResponse = await client.GetAsync(DiagnosisUrl(scenario.DiagnosisId));
        Assert.Equal(HttpStatusCode.OK, preservedResponse.StatusCode);
        var preserved = await ReadJsonAsync(preservedResponse);
        Assert.Equal("Diagnóstico original", preserved.GetProperty("title").GetString());
        Assert.Equal("Contexto persistido", preserved.GetProperty("context").GetString());
        Assert.Equal("InProgress", preserved.GetProperty("status").GetString());
        var persistedAssessment = Assert.Single(
            preserved.GetProperty("assessments").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == scenario.AssessmentId);
        Assert.Equal(savedAnswer, persistedAssessment.GetProperty("q01").GetString());

        using var opportunityList = await client.GetAsync(OpportunitiesUrl(scenario.DiagnosisId));
        Assert.Equal(HttpStatusCode.OK, opportunityList.StatusCode);
        Assert.Contains(
            (await ReadJsonAsync(opportunityList)).EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == opportunityId &&
                item.GetProperty("title").GetString() == "Oportunidade original");

        using var allowedEdit = await client.PutAsJsonAsync(
            DiagnosisUrl(scenario.DiagnosisId),
            DiagnosisInput(scenario.ClientId, "Título atualizado após reabertura", "Contexto persistido"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, allowedEdit.StatusCode);
        var edited = await ReadJsonAsync(allowedEdit);
        Assert.Equal("Título atualizado após reabertura", edited.GetProperty("title").GetString());
        Assert.Equal("Contexto persistido", edited.GetProperty("context").GetString());
    }

    [Fact]
    public async Task CompletedDiagnosis_RejectsContentMutationsAndRetainsThePersistedSnapshot()
    {
        using var client = await CreateApiClientAsync();
        var scenario = await CreateScenarioAsync(client, createSecondSector: true);
        var additionalSectorId = scenario.AdditionalSectorId!.Value;
        var bottleneckId = await CreateBottleneckAsync(client, scenario);
        var spareBottleneckId = await CreateBottleneckAsync(client, scenario, "Gargalo sem vínculo");
        var opportunityId = await CreateOpportunityAsync(client, scenario, bottleneckId);
        using var additionalAssessmentResponse = await client.PostAsJsonAsync(
            $"/api/diagnoses/{scenario.DiagnosisId}/assessments",
            new { clientSectorId = additionalSectorId }, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, additionalAssessmentResponse.StatusCode);
        var additionalAssessmentId = (await ReadJsonAsync(additionalAssessmentResponse)).GetProperty("id").GetGuid();
        const string savedAnswer = "Resposta original";
        await SaveAnswersAsync(client, scenario.DiagnosisId, scenario.AssessmentId, savedAnswer);
        var phases = await GetPhasesAsync(client, scenario.DiagnosisId);
        var phase = phases[0];

        using var completed = await SetStatusAsync(client, scenario.DiagnosisId, "Completed");
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);

        using var diagnosisEdit = await client.PutAsJsonAsync(
            DiagnosisUrl(scenario.DiagnosisId),
            DiagnosisInput(scenario.ClientId, "Edição bloqueada", "Contexto original"),
            JsonOptions);
        using var assessmentAdd = await client.PostAsJsonAsync(
            $"/api/diagnoses/{scenario.DiagnosisId}/assessments",
            new { clientSectorId = additionalSectorId }, JsonOptions);
        using var assessmentDelete = await client.DeleteAsync(AssessmentUrl(scenario.DiagnosisId, additionalAssessmentId));
        using var answersEdit = await client.PutAsJsonAsync(
            AnswersUrl(scenario.DiagnosisId, scenario.AssessmentId),
            Answers("Resposta que não pode ser salva"), JsonOptions);
        using var bottleneckAdd = await client.PostAsJsonAsync(
            BottlenecksUrl(scenario.DiagnosisId), BottleneckInput(scenario.AssessmentId, "Novo gargalo"), JsonOptions);
        using var bottleneckEdit = await client.PutAsJsonAsync(
            BottleneckUrl(scenario.DiagnosisId, bottleneckId), BottleneckInput(scenario.AssessmentId, "Gargalo alterado"), JsonOptions);
        using var bottleneckDelete = await client.DeleteAsync(BottleneckUrl(scenario.DiagnosisId, bottleneckId));
        using var spareBottleneckDelete = await client.DeleteAsync(BottleneckUrl(scenario.DiagnosisId, spareBottleneckId));
        using var opportunityAdd = await client.PostAsJsonAsync(
            OpportunitiesUrl(scenario.DiagnosisId), OpportunityInput(bottleneckId, "Nova oportunidade"), JsonOptions);
        using var opportunityEdit = await client.PutAsJsonAsync(
            OpportunityUrl(scenario.DiagnosisId, opportunityId), OpportunityInput(bottleneckId, "Oportunidade alterada"), JsonOptions);
        using var opportunityDelete = await client.DeleteAsync(OpportunityUrl(scenario.DiagnosisId, opportunityId));
        using var phaseAdd = await client.PostAsJsonAsync(
            PhasesUrl(scenario.DiagnosisId), new { name = "Nova fase", description = (string?)null }, JsonOptions);
        using var phaseEdit = await client.PutAsJsonAsync(
            PhaseUrl(scenario.DiagnosisId, phase.GetProperty("id").GetGuid()), new { name = "Fase alterada", description = (string?)null }, JsonOptions);
        using var phaseOrder = await client.PutAsJsonAsync(
            $"{PhasesUrl(scenario.DiagnosisId)}/order",
            new { phaseIds = phases.Reverse().Select(item => item.GetProperty("id").GetGuid()).ToArray() }, JsonOptions);
        using var reportEdit = await client.PutAsJsonAsync(
            $"{DiagnosisUrl(scenario.DiagnosisId)}/report",
            new { executiveSummary = "Resumo não salvo", generalRecommendations = "", nextSteps = "" }, JsonOptions);

        Assert.All(
            new[]
            {
                diagnosisEdit.StatusCode,
                assessmentAdd.StatusCode,
                assessmentDelete.StatusCode,
                answersEdit.StatusCode,
                bottleneckAdd.StatusCode,
                bottleneckEdit.StatusCode,
                bottleneckDelete.StatusCode,
                spareBottleneckDelete.StatusCode,
                opportunityAdd.StatusCode,
                opportunityEdit.StatusCode,
                opportunityDelete.StatusCode,
                phaseAdd.StatusCode,
                phaseEdit.StatusCode,
                phaseOrder.StatusCode,
                reportEdit.StatusCode
            },
            statusCode => Assert.Equal(HttpStatusCode.Conflict, statusCode));

        using var diagnosisResponse = await client.GetAsync(DiagnosisUrl(scenario.DiagnosisId));
        Assert.Equal(HttpStatusCode.OK, diagnosisResponse.StatusCode);
        var persistedDiagnosis = await ReadJsonAsync(diagnosisResponse);
        Assert.Equal("Diagnóstico original", persistedDiagnosis.GetProperty("title").GetString());
        Assert.Equal("Completed", persistedDiagnosis.GetProperty("status").GetString());
        var persistedAssessment = Assert.Single(
            persistedDiagnosis.GetProperty("assessments").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == scenario.AssessmentId);
        Assert.Equal(savedAnswer, persistedAssessment.GetProperty("q01").GetString());

        using var bottleneckList = await client.GetAsync(BottlenecksUrl(scenario.DiagnosisId));
        Assert.Equal(HttpStatusCode.OK, bottleneckList.StatusCode);
        var persistedBottlenecks = (await ReadJsonAsync(bottleneckList)).EnumerateArray().ToArray();
        Assert.Contains(persistedBottlenecks, item => item.GetProperty("id").GetGuid() == bottleneckId && item.GetProperty("title").GetString() == "Gargalo original");
        Assert.Contains(persistedBottlenecks, item => item.GetProperty("id").GetGuid() == spareBottleneckId && item.GetProperty("title").GetString() == "Gargalo sem vínculo");

        using var opportunityList = await client.GetAsync(OpportunitiesUrl(scenario.DiagnosisId));
        Assert.Equal(HttpStatusCode.OK, opportunityList.StatusCode);
        Assert.Equal("Oportunidade original", Assert.Single((await ReadJsonAsync(opportunityList)).EnumerateArray()).GetProperty("title").GetString());

        var persistedPhases = await GetPhasesAsync(client, scenario.DiagnosisId);
        Assert.Equal(
            phases.Select(item => item.GetProperty("id").GetGuid()),
            persistedPhases.Select(item => item.GetProperty("id").GetGuid()));
        Assert.Equal(
            phases.Select(item => item.GetProperty("name").GetString()),
            persistedPhases.Select(item => item.GetProperty("name").GetString()));
    }

    private async Task<HttpClient> CreateApiClientAsync()
    {
        await factory.ApplyMigrationsAsync();
        return factory.CreateClient();
    }

    private static async Task<Scenario> CreateScenarioAsync(
        HttpClient client,
        bool includePrimaryObjective = true,
        bool createSecondSector = false)
    {
        var token = Guid.NewGuid().ToString("N");
        using var clientResponse = await client.PostAsJsonAsync(
            "/api/clients",
            new
            {
                companyName = $"Empresa {token}",
                segment = "Tecnologia",
                contactName = (string?)null,
                email = (string?)null,
                phone = (string?)null,
                notes = (string?)null
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, clientResponse.StatusCode);
        var clientId = (await ReadJsonAsync(clientResponse)).GetProperty("id").GetGuid();

        object[] objectives = includePrimaryObjective
            ? [new { code = "ReduceCosts", isPrimary = true, customDescription = (string?)null }]
            : [];
        using var diagnosisResponse = await client.PostAsJsonAsync(
            "/api/diagnoses",
            new
            {
                clientId,
                title = "Diagnóstico original",
                assessmentDate = DateOnly.FromDateTime(DateTime.Today),
                context = "Contexto persistido",
                objectives
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, diagnosisResponse.StatusCode);
        var diagnosisId = (await ReadJsonAsync(diagnosisResponse)).GetProperty("id").GetGuid();

        var sectorId = await CreateSectorAsync(client, clientId, "Operação");
        using var assessmentResponse = await client.PostAsJsonAsync(
            $"/api/diagnoses/{diagnosisId}/assessments",
            new { clientSectorId = sectorId },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, assessmentResponse.StatusCode);
        var assessmentId = (await ReadJsonAsync(assessmentResponse)).GetProperty("id").GetGuid();
        var additionalSectorId = createSecondSector
            ? await CreateSectorAsync(client, clientId, "Financeiro")
            : (Guid?)null;
        return new Scenario(clientId, diagnosisId, assessmentId, additionalSectorId);
    }

    private static async Task<Guid> CreateSectorAsync(HttpClient client, Guid clientId, string name)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/clients/{clientId}/sectors",
            new { name, responsibleName = (string?)null, peopleCount = (int?)null },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateBottleneckAsync(HttpClient client, Scenario scenario, string title = "Gargalo original")
    {
        using var response = await client.PostAsJsonAsync(
            BottlenecksUrl(scenario.DiagnosisId),
            BottleneckInput(scenario.AssessmentId, title),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateOpportunityAsync(HttpClient client, Scenario scenario, Guid bottleneckId)
    {
        using var response = await client.PostAsJsonAsync(
            OpportunitiesUrl(scenario.DiagnosisId),
            OpportunityInput(bottleneckId, "Oportunidade original"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task SaveAnswersAsync(HttpClient client, Guid diagnosisId, Guid assessmentId, string answer)
    {
        using var response = await client.PutAsJsonAsync(
            AnswersUrl(diagnosisId, assessmentId), Answers(answer), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<string?> ReadDiagnosisStatusAsync(HttpClient client, Guid diagnosisId)
    {
        using var response = await client.GetAsync(DiagnosisUrl(diagnosisId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("status").GetString();
    }

    private static async Task<HttpResponseMessage> SetStatusAsync(HttpClient client, Guid diagnosisId, string status) =>
        await client.PutAsJsonAsync($"{DiagnosisUrl(diagnosisId)}/status", new { status }, JsonOptions);

    private static async Task<JsonElement[]> GetPhasesAsync(HttpClient client, Guid diagnosisId)
    {
        using var response = await client.GetAsync(PhasesUrl(diagnosisId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ReadJsonAsync(response)).EnumerateArray().ToArray();
    }

    private static object DiagnosisInput(Guid clientId, string title, string context) => new
    {
        clientId,
        title,
        assessmentDate = DateOnly.FromDateTime(DateTime.Today),
        context,
        objectives = new[]
        {
            new { code = "ReduceCosts", isPrimary = true, customDescription = (string?)null }
        }
    };

    private static object BottleneckInput(Guid assessmentId, string title) => new
    {
        sectorAssessmentId = assessmentId,
        title,
        description = "A equipe realiza a conferência manualmente.",
        category = "ManualWork",
        impact = 3,
        frequency = "Diária",
        evidence = "Amostra da rotina",
        probableCause = "Sistemas separados",
        estimatedMonthlyHours = (decimal?)null,
        notes = (string?)null
    };

    private static object OpportunityInput(Guid bottleneckId, string title) => new
    {
        title,
        solutionDescription = "Automatizar a conferência.",
        type = "Automation",
        expectedBenefit = "Reduzir retrabalho manual",
        impact = 4,
        effort = 2,
        urgency = 3,
        dependencies = (string?)null,
        aiDataNeeds = (string?)null,
        needsHumanReview = (bool?)null,
        phaseId = (Guid?)null,
        bottleneckIds = new[] { bottleneckId }
    };

    private static Dictionary<string, string?> Answers(string answer) =>
        Enumerable.Range(1, 10).ToDictionary(
            index => $"q{index:00}",
            index => index == 1 ? answer : null,
            StringComparer.Ordinal);

    private static string DiagnosisUrl(Guid diagnosisId) => $"/api/diagnoses/{diagnosisId}";
    private static string BottlenecksUrl(Guid diagnosisId) => $"{DiagnosisUrl(diagnosisId)}/bottlenecks";
    private static string BottleneckUrl(Guid diagnosisId, Guid bottleneckId) => $"{BottlenecksUrl(diagnosisId)}/{bottleneckId}";
    private static string OpportunitiesUrl(Guid diagnosisId) => $"{DiagnosisUrl(diagnosisId)}/opportunities";
    private static string OpportunityUrl(Guid diagnosisId, Guid opportunityId) => $"{OpportunitiesUrl(diagnosisId)}/{opportunityId}";
    private static string PhasesUrl(Guid diagnosisId) => $"{DiagnosisUrl(diagnosisId)}/phases";
    private static string PhaseUrl(Guid diagnosisId, Guid phaseId) => $"{PhasesUrl(diagnosisId)}/{phaseId}";
    private static string AnswersUrl(Guid diagnosisId, Guid assessmentId) =>
        $"{DiagnosisUrl(diagnosisId)}/assessments/{assessmentId}/answers";
    private static string AssessmentUrl(Guid diagnosisId, Guid assessmentId) =>
        $"{DiagnosisUrl(diagnosisId)}/assessments/{assessmentId}";

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private sealed record Scenario(Guid ClientId, Guid DiagnosisId, Guid AssessmentId, Guid? AdditionalSectorId);
}
