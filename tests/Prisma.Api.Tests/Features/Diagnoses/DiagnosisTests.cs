using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prisma.Api.Tests.Infrastructure;

namespace Prisma.Api.Tests.Features.Diagnoses;

public sealed class DiagnosisTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateDiagnosis_PersistsClientObjectivesAssessmentDateAndDraftStatus()
    {
        using var client = await CreateApiClientAsync();
        var token = NewToken();
        var clientId = await CreateClientAsync(client, token);
        var assessmentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2));
        var input = DiagnosisInput(
            clientId,
            $"Diagnóstico {token}",
            assessmentDate,
            context: "Contexto inicial",
            Objective("ReduceCosts", isPrimary: true),
            Objective("ImproveProductivity", isPrimary: false));

        using var response = await client.PostAsJsonAsync("/api/diagnoses", input, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        var diagnosisId = created.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, diagnosisId);
        Assert.Equal(clientId, created.GetProperty("clientId").GetGuid());
        Assert.Equal($"Diagnóstico {token}", created.GetProperty("title").GetString());
        Assert.Equal(assessmentDate.ToString("yyyy-MM-dd"), created.GetProperty("assessmentDate").GetString());
        Assert.Equal("Draft", created.GetProperty("status").GetString());
        Assert.Equal("Contexto inicial", created.GetProperty("context").GetString());
        AssertObjectives(created.GetProperty("objectives"));

        using var reloadResponse = await client.GetAsync($"/api/diagnoses/{diagnosisId}");
        Assert.Equal(HttpStatusCode.OK, reloadResponse.StatusCode);
        var persisted = await ReadJsonAsync(reloadResponse);
        Assert.Equal("Draft", persisted.GetProperty("status").GetString());
        Assert.Equal(clientId, persisted.GetProperty("clientId").GetGuid());
        AssertObjectives(persisted.GetProperty("objectives"));
    }

    [Fact]
    public async Task CreateDiagnosis_RejectsUnknownClient()
    {
        using var client = await CreateApiClientAsync();
        var input = DiagnosisInput(
            Guid.NewGuid(),
            "Diagnóstico sem cliente",
            DateOnly.FromDateTime(DateTime.Today),
            context: null,
            Objective("ReduceCosts", isPrimary: true));

        using var response = await client.PostAsJsonAsync("/api/diagnoses", input, JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDiagnosis_RejectsMoreThanOnePrimaryObjective()
    {
        using var client = await CreateApiClientAsync();
        var clientId = await CreateClientAsync(client, NewToken());
        var input = DiagnosisInput(
            clientId,
            "Objetivos incompatíveis",
            DateOnly.FromDateTime(DateTime.Today),
            context: null,
            Objective("ReduceCosts", isPrimary: true),
            Objective("ImproveService", isPrimary: true));

        using var response = await client.PostAsJsonAsync("/api/diagnoses", input, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDiagnosis_RequiresDescriptionForOtherObjective()
    {
        using var client = await CreateApiClientAsync();
        var clientId = await CreateClientAsync(client, NewToken());
        var input = DiagnosisInput(
            clientId,
            "Objetivo personalizado incompleto",
            DateOnly.FromDateTime(DateTime.Today),
            context: null,
            Objective("Other", isPrimary: true, customDescription: null));

        using var response = await client.PostAsJsonAsync("/api/diagnoses", input, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddAssessment_RejectsSectorOwnedByAnotherClient()
    {
        using var client = await CreateApiClientAsync();
        var firstClientId = await CreateClientAsync(client, NewToken());
        var otherClientId = await CreateClientAsync(client, NewToken());
        var diagnosisId = await CreateDiagnosisAsync(client, firstClientId, NewToken());
        var otherClientSectorId = await CreateSectorAsync(client, otherClientId, "Financeiro");

        using var response = await client.PostAsJsonAsync(
            $"/api/diagnoses/{diagnosisId}/assessments",
            new { clientSectorId = otherClientSectorId },
            JsonOptions);

        AssertOwnedResourceConflictOrNotFound(response.StatusCode);
    }

    [Fact]
    public async Task UpdateDiagnosis_CannotChangeClientAfterAssessmentExists()
    {
        using var client = await CreateApiClientAsync();
        var originalClientId = await CreateClientAsync(client, NewToken());
        var replacementClientId = await CreateClientAsync(client, NewToken());
        var sectorId = await CreateSectorAsync(client, originalClientId, "Atendimento");
        var diagnosisId = await CreateDiagnosisAsync(client, originalClientId, NewToken());

        using var assessmentResponse = await client.PostAsJsonAsync(
            $"/api/diagnoses/{diagnosisId}/assessments",
            new { clientSectorId = sectorId },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, assessmentResponse.StatusCode);

        var update = DiagnosisInput(
            replacementClientId,
            "Tentativa de trocar cliente",
            DateOnly.FromDateTime(DateTime.Today),
            context: "Não deve substituir o diagnóstico original",
            Objective("ReduceCosts", isPrimary: true));
        using var updateResponse = await client.PutAsJsonAsync($"/api/diagnoses/{diagnosisId}", update, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
        using var reloadResponse = await client.GetAsync($"/api/diagnoses/{diagnosisId}");
        Assert.Equal(HttpStatusCode.OK, reloadResponse.StatusCode);
        var persisted = await ReadJsonAsync(reloadResponse);
        Assert.Equal(originalClientId, persisted.GetProperty("clientId").GetGuid());
    }

    [Fact]
    public async Task UpdateDiagnosis_CanChangeClientBeforeAnyAssessmentExists()
    {
        using var client = await CreateApiClientAsync();
        var originalClientId = await CreateClientAsync(client, NewToken());
        var replacementClientId = await CreateClientAsync(client, NewToken());
        var diagnosisId = await CreateDiagnosisAsync(client, originalClientId, NewToken());
        var update = DiagnosisInput(
            replacementClientId,
            "Diagnóstico associado ao novo cliente",
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            context: "A associação pode ser alterada antes de avaliar setores.",
            Objective("ImproveProductivity", isPrimary: true));

        using var updateResponse = await client.PutAsJsonAsync($"/api/diagnoses/{diagnosisId}", update, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await ReadJsonAsync(updateResponse);
        Assert.Equal(replacementClientId, updated.GetProperty("clientId").GetGuid());
        Assert.Equal("Diagnóstico associado ao novo cliente", updated.GetProperty("title").GetString());
        Assert.StartsWith("Empresa ", updated.GetProperty("clientName").GetString());
        var objective = Assert.Single(updated.GetProperty("objectives").EnumerateArray());
        Assert.Equal("ImproveProductivity", objective.GetProperty("code").GetString());
        Assert.True(objective.GetProperty("isPrimary").GetBoolean());

        using var reloadResponse = await client.GetAsync($"/api/diagnoses/{diagnosisId}");
        Assert.Equal(HttpStatusCode.OK, reloadResponse.StatusCode);
        var persisted = await ReadJsonAsync(reloadResponse);
        Assert.Equal(replacementClientId, persisted.GetProperty("clientId").GetGuid());
        Assert.Equal("Draft", persisted.GetProperty("status").GetString());
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
                segment = "Tecnologia",
                contactName = (string?)null,
                email = (string?)null,
                phone = (string?)null,
                notes = (string?)null
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        return created.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateSectorAsync(HttpClient client, Guid clientId, string name)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/clients/{clientId}/sectors",
            new { name, responsibleName = (string?)null, peopleCount = (int?)null },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        Assert.Equal(clientId, created.GetProperty("clientId").GetGuid());
        return created.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateDiagnosisAsync(HttpClient client, Guid clientId, string token)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/diagnoses",
            DiagnosisInput(
                clientId,
                $"Diagnóstico {token}",
                DateOnly.FromDateTime(DateTime.Today),
                context: null,
                Objective("ReduceCosts", isPrimary: true)),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        return created.GetProperty("id").GetGuid();
    }

    private static object DiagnosisInput(
        Guid clientId,
        string title,
        DateOnly assessmentDate,
        string? context,
        params object[] objectives) => new
        {
            clientId,
            title,
            assessmentDate,
            context,
            objectives
        };

    private static object Objective(string code, bool isPrimary, string? customDescription = null) => new
    {
        code,
        isPrimary,
        customDescription
    };

    private static void AssertObjectives(JsonElement objectives)
    {
        var items = objectives.EnumerateArray().ToArray();
        Assert.Equal(2, items.Length);
        Assert.Contains(items, item =>
            item.GetProperty("code").GetString() == "ReduceCosts" &&
            item.GetProperty("isPrimary").GetBoolean());
        Assert.Contains(items, item =>
            item.GetProperty("code").GetString() == "ImproveProductivity" &&
            !item.GetProperty("isPrimary").GetBoolean());
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private static void AssertOwnedResourceConflictOrNotFound(HttpStatusCode statusCode) =>
        Assert.Contains(statusCode, new[] { HttpStatusCode.Conflict, HttpStatusCode.NotFound });

    private static string NewToken() => Guid.NewGuid().ToString("N");
}
