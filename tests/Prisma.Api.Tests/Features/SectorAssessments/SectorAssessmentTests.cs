using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prisma.Api.Tests.Infrastructure;

namespace Prisma.Api.Tests.Features.SectorAssessments;

public sealed class SectorAssessmentTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Answers_AreInitiallyEmptyAndPersistOnlyAfterExplicitSave()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithAssessmentsAsync(client, "Atendimento");

        var initial = await GetAssessmentAsync(client, setup.DiagnosisId, setup.AssessmentIds[0]);
        Assert.Equal(0, initial.GetProperty("answeredCount").GetInt32());
        Assert.All(Enumerable.Range(1, 10), index => Assert.Null(initial.GetProperty($"q{index:00}").GetString()));

        var answers = Answers("Mapeamento de atividades", "   ", null, null, null, null, null, null, null, "Resultado esperado");
        using var saveResponse = await client.PutAsJsonAsync(
            AnswersUrl(setup.DiagnosisId, setup.AssessmentIds[0]),
            answers,
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        var saved = await ReadJsonAsync(saveResponse);
        Assert.Equal(2, saved.GetProperty("answeredCount").GetInt32());
        Assert.Equal("Mapeamento de atividades", saved.GetProperty("q01").GetString());
        Assert.Null(saved.GetProperty("q02").GetString());
        Assert.Equal("Resultado esperado", saved.GetProperty("q10").GetString());

        var reloaded = await GetAssessmentAsync(client, setup.DiagnosisId, setup.AssessmentIds[0]);
        Assert.Equal(2, reloaded.GetProperty("answeredCount").GetInt32());
        Assert.Equal("Mapeamento de atividades", reloaded.GetProperty("q01").GetString());
        Assert.Equal("Resultado esperado", reloaded.GetProperty("q10").GetString());
    }

    [Fact]
    public async Task Answers_PutReplacesTheWholeSnapshotAndNormalizesBlankText()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithAssessmentsAsync(client, "Financeiro");
        var assessmentId = setup.AssessmentIds[0];

        using var firstSave = await client.PutAsJsonAsync(
            AnswersUrl(setup.DiagnosisId, assessmentId),
            Answers("Resposta antiga", null, "Também será removida", null, null, null, null, null, null, null),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, firstSave.StatusCode);

        using var replacementSave = await client.PutAsJsonAsync(
            AnswersUrl(setup.DiagnosisId, assessmentId),
            Answers(null, "Resposta atual", "  ", null, null, null, null, null, null, null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, replacementSave.StatusCode);
        var replaced = await ReadJsonAsync(replacementSave);
        Assert.Equal(1, replaced.GetProperty("answeredCount").GetInt32());
        Assert.Null(replaced.GetProperty("q01").GetString());
        Assert.Equal("Resposta atual", replaced.GetProperty("q02").GetString());
        Assert.Null(replaced.GetProperty("q03").GetString());

        var reloaded = await GetAssessmentAsync(client, setup.DiagnosisId, assessmentId);
        Assert.Equal(1, reloaded.GetProperty("answeredCount").GetInt32());
        Assert.Null(reloaded.GetProperty("q01").GetString());
        Assert.Equal("Resposta atual", reloaded.GetProperty("q02").GetString());
        Assert.Null(reloaded.GetProperty("q03").GetString());
    }

    [Fact]
    public async Task Answers_AreIsolatedBetweenSectorAssessments()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithAssessmentsAsync(client, "Comercial", "Operação");

        using var firstSave = await client.PutAsJsonAsync(
            AnswersUrl(setup.DiagnosisId, setup.AssessmentIds[0]),
            Answers("Processos comerciais", null, null, null, null, null, null, null, null, null),
            JsonOptions);
        using var secondSave = await client.PutAsJsonAsync(
            AnswersUrl(setup.DiagnosisId, setup.AssessmentIds[1]),
            Answers("Rotinas operacionais", "Ferramenta própria", null, null, null, null, null, null, null, null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, firstSave.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondSave.StatusCode);

        var firstAssessment = await GetAssessmentAsync(client, setup.DiagnosisId, setup.AssessmentIds[0]);
        var secondAssessment = await GetAssessmentAsync(client, setup.DiagnosisId, setup.AssessmentIds[1]);
        Assert.Equal("Comercial", firstAssessment.GetProperty("sectorNameSnapshot").GetString());
        Assert.Equal("Processos comerciais", firstAssessment.GetProperty("q01").GetString());
        Assert.Equal(1, firstAssessment.GetProperty("answeredCount").GetInt32());
        Assert.Equal("Operação", secondAssessment.GetProperty("sectorNameSnapshot").GetString());
        Assert.Equal("Rotinas operacionais", secondAssessment.GetProperty("q01").GetString());
        Assert.Equal("Ferramenta própria", secondAssessment.GetProperty("q02").GetString());
        Assert.Equal(2, secondAssessment.GetProperty("answeredCount").GetInt32());
    }

    [Fact]
    public async Task Answers_ProgressCountsExactlyFromZeroThroughTen()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithAssessmentsAsync(client, "Administrativo");
        var assessmentId = setup.AssessmentIds[0];

        var empty = await GetAssessmentAsync(client, setup.DiagnosisId, assessmentId);
        Assert.Equal(0, empty.GetProperty("answeredCount").GetInt32());

        using var saveResponse = await client.PutAsJsonAsync(
            AnswersUrl(setup.DiagnosisId, assessmentId),
            Answers(
                "Resposta 1", "Resposta 2", "Resposta 3", "Resposta 4", "Resposta 5",
                "Resposta 6", "Resposta 7", "Resposta 8", "Resposta 9", "Resposta 10"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        var full = await ReadJsonAsync(saveResponse);
        Assert.Equal(10, full.GetProperty("answeredCount").GetInt32());
        Assert.Equal("Resposta 10", full.GetProperty("q10").GetString());
    }

    [Fact]
    public async Task RemoveAssessment_RequiresNoSavedAnswersOrBottlenecks()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithAssessmentsAsync(client, "Financeiro", "Atendimento", "Operação");
        var answeredAssessmentId = setup.AssessmentIds[0];
        var bottleneckAssessmentId = setup.AssessmentIds[1];
        var emptyAssessmentId = setup.AssessmentIds[2];

        using var answerSave = await client.PutAsJsonAsync(
            AnswersUrl(setup.DiagnosisId, answeredAssessmentId),
            Answers("Resposta persistida", null, null, null, null, null, null, null, null, null),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, answerSave.StatusCode);

        using var bottleneckCreate = await client.PostAsJsonAsync(
            $"/api/diagnoses/{setup.DiagnosisId}/bottlenecks",
            new
            {
                sectorAssessmentId = bottleneckAssessmentId,
                title = "Gargalo existente",
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
        Assert.Equal(HttpStatusCode.Created, bottleneckCreate.StatusCode);

        using var answeredDelete = await client.DeleteAsync(AssessmentUrl(setup.DiagnosisId, answeredAssessmentId));
        using var linkedDelete = await client.DeleteAsync(AssessmentUrl(setup.DiagnosisId, bottleneckAssessmentId));
        using var emptyDelete = await client.DeleteAsync(AssessmentUrl(setup.DiagnosisId, emptyAssessmentId));

        Assert.Equal(HttpStatusCode.Conflict, answeredDelete.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, linkedDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, emptyDelete.StatusCode);

        using var diagnosisResponse = await client.GetAsync($"/api/diagnoses/{setup.DiagnosisId}");
        Assert.Equal(HttpStatusCode.OK, diagnosisResponse.StatusCode);
        var remainingIds = (await ReadJsonAsync(diagnosisResponse))
            .GetProperty("assessments")
            .EnumerateArray()
            .Select(assessment => assessment.GetProperty("id").GetGuid())
            .ToArray();
        Assert.Equal(new[] { answeredAssessmentId, bottleneckAssessmentId }.Order(), remainingIds.Order());
    }

    private async Task<HttpClient> CreateApiClientAsync()
    {
        await factory.ApplyMigrationsAsync();
        return factory.CreateClient();
    }

    private static async Task<(Guid DiagnosisId, Guid[] AssessmentIds)> CreateDiagnosisWithAssessmentsAsync(
        HttpClient client,
        params string[] sectorNames)
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

        var assessmentIds = new List<Guid>();
        foreach (var sectorName in sectorNames)
        {
            using var sectorResponse = await client.PostAsJsonAsync(
                $"/api/clients/{clientId}/sectors",
                new { name = sectorName, responsibleName = (string?)null, peopleCount = (int?)null },
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
            assessmentIds.Add(assessment.GetProperty("id").GetGuid());
        }

        return (diagnosisId, assessmentIds.ToArray());
    }

    private static Dictionary<string, string?> Answers(params string?[] values)
    {
        Assert.Equal(10, values.Length);
        return values.Select((value, index) => new KeyValuePair<string, string?>($"q{index + 1:00}", value))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static string AnswersUrl(Guid diagnosisId, Guid assessmentId) =>
        $"/api/diagnoses/{diagnosisId}/assessments/{assessmentId}/answers";

    private static string AssessmentUrl(Guid diagnosisId, Guid assessmentId) =>
        $"/api/diagnoses/{diagnosisId}/assessments/{assessmentId}";

    private static async Task<JsonElement> GetAssessmentAsync(HttpClient client, Guid diagnosisId, Guid assessmentId)
    {
        using var response = await client.GetAsync($"/api/diagnoses/{diagnosisId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var diagnosis = await ReadJsonAsync(response);
        return Assert.Single(
            diagnosis.GetProperty("assessments").EnumerateArray(),
            assessment => assessment.GetProperty("id").GetGuid() == assessmentId);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }
}
