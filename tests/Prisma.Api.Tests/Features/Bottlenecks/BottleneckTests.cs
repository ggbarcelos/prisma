using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prisma.Api.Tests.Infrastructure;

namespace Prisma.Api.Tests.Features.Bottlenecks;

public sealed class BottleneckTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateBottleneck_RejectsMissingRequiredValuesAndInvalidRanges()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithAssessmentAsync(client);

        using var missingTitle = await client.PostAsJsonAsync(
            BottlenecksUrl(setup.DiagnosisId),
            BottleneckInput(setup.AssessmentId, title: "   "),
            JsonOptions);
        using var invalidCategory = await client.PostAsJsonAsync(
            BottlenecksUrl(setup.DiagnosisId),
            BottleneckInput(setup.AssessmentId, category: "UnknownCategory"),
            JsonOptions);
        using var belowImpactRange = await client.PostAsJsonAsync(
            BottlenecksUrl(setup.DiagnosisId),
            BottleneckInput(setup.AssessmentId, impact: 0),
            JsonOptions);
        using var aboveImpactRange = await client.PostAsJsonAsync(
            BottlenecksUrl(setup.DiagnosisId),
            BottleneckInput(setup.AssessmentId, impact: 6),
            JsonOptions);
        using var missingAssessment = await client.PostAsJsonAsync(
            BottlenecksUrl(setup.DiagnosisId),
            BottleneckInput(Guid.Empty),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, missingTitle.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidCategory.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, belowImpactRange.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, aboveImpactRange.StatusCode);
        AssertOwnedResourceConflictOrNotFound(missingAssessment.StatusCode);
    }

    [Fact]
    public async Task CreateBottleneck_AllowsOptionalHoursButRejectsNegativeValues()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithAssessmentAsync(client);

        using var negativeHours = await client.PostAsJsonAsync(
            BottlenecksUrl(setup.DiagnosisId),
            BottleneckInput(setup.AssessmentId, estimatedMonthlyHours: -0.5m),
            JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, negativeHours.StatusCode);

        using var emptyHours = await client.PostAsJsonAsync(
            BottlenecksUrl(setup.DiagnosisId),
            BottleneckInput(setup.AssessmentId, category: "Other", estimatedMonthlyHours: null),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, emptyHours.StatusCode);
        var withoutEstimate = await ReadJsonAsync(emptyHours);
        Assert.Equal("Other", withoutEstimate.GetProperty("category").GetString());
        Assert.Equal(JsonValueKind.Null, withoutEstimate.GetProperty("estimatedMonthlyHours").ValueKind);

        using var zeroHours = await client.PostAsJsonAsync(
            BottlenecksUrl(setup.DiagnosisId),
            BottleneckInput(setup.AssessmentId, estimatedMonthlyHours: 0m),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, zeroHours.StatusCode);
        var withZero = await ReadJsonAsync(zeroHours);
        Assert.Equal(0m, withZero.GetProperty("estimatedMonthlyHours").GetDecimal());
    }

    [Fact]
    public async Task CreateBottleneck_RejectsAssessmentFromAnotherDiagnosis()
    {
        using var client = await CreateApiClientAsync();
        var first = await CreateDiagnosisWithAssessmentAsync(client);
        var second = await CreateDiagnosisWithAssessmentAsync(client);

        using var response = await client.PostAsJsonAsync(
            BottlenecksUrl(first.DiagnosisId),
            BottleneckInput(second.AssessmentId),
            JsonOptions);

        AssertOwnedResourceConflictOrNotFound(response.StatusCode);
    }

    [Fact]
    public async Task Bottlenecks_CanBeListedUpdatedAndDeletedWithinTheirDiagnosis()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithAssessmentAsync(client);
        var bottleneckId = await CreateBottleneckAsync(client, setup.DiagnosisId, setup.AssessmentId);

        using var listResponse = await client.GetAsync(BottlenecksUrl(setup.DiagnosisId));
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listed = await ReadJsonAsync(listResponse);
        var bottleneck = Assert.Single(listed.EnumerateArray());
        Assert.Equal(bottleneckId, bottleneck.GetProperty("id").GetGuid());
        Assert.Equal(setup.DiagnosisId, bottleneck.GetProperty("diagnosisId").GetGuid());

        using var updateResponse = await client.PutAsJsonAsync(
            BottleneckUrl(setup.DiagnosisId, bottleneckId),
            BottleneckInput(
                setup.AssessmentId,
                title: "  Integração de dados  ",
                category: "MissingIntegration",
                impact: 5,
                estimatedMonthlyHours: 12.5m),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await ReadJsonAsync(updateResponse);
        Assert.Equal("Integração de dados", updated.GetProperty("title").GetString());
        Assert.Equal("MissingIntegration", updated.GetProperty("category").GetString());
        Assert.Equal(5, updated.GetProperty("impact").GetInt32());
        Assert.Equal(12.5m, updated.GetProperty("estimatedMonthlyHours").GetDecimal());

        using var deleteResponse = await client.DeleteAsync(BottleneckUrl(setup.DiagnosisId, bottleneckId));
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var emptyListResponse = await client.GetAsync(BottlenecksUrl(setup.DiagnosisId));
        Assert.Equal(HttpStatusCode.OK, emptyListResponse.StatusCode);
        var emptyList = await ReadJsonAsync(emptyListResponse);
        Assert.Empty(emptyList.EnumerateArray());
    }

    [Fact]
    public async Task BottleneckRoutes_DoNotExposeRecordsAcrossDiagnoses()
    {
        using var client = await CreateApiClientAsync();
        var first = await CreateDiagnosisWithAssessmentAsync(client);
        var second = await CreateDiagnosisWithAssessmentAsync(client);
        var bottleneckId = await CreateBottleneckAsync(client, first.DiagnosisId, first.AssessmentId);

        using var listResponse = await client.GetAsync(BottlenecksUrl(second.DiagnosisId));
        using var updateResponse = await client.PutAsJsonAsync(
            BottleneckUrl(second.DiagnosisId, bottleneckId),
            BottleneckInput(second.AssessmentId, title: "Alteração cruzada"),
            JsonOptions);
        using var deleteResponse = await client.DeleteAsync(BottleneckUrl(second.DiagnosisId, bottleneckId));
        using var missingDiagnosisResponse = await client.GetAsync(BottlenecksUrl(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Empty((await ReadJsonAsync(listResponse)).EnumerateArray());
        AssertOwnedResourceConflictOrNotFound(updateResponse.StatusCode);
        AssertOwnedResourceConflictOrNotFound(deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingDiagnosisResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteBottleneck_IsBlockedWhileAnOpportunityReferencesIt()
    {
        using var client = await CreateApiClientAsync();
        var setup = await CreateDiagnosisWithAssessmentAsync(client);
        var bottleneckId = await CreateBottleneckAsync(client, setup.DiagnosisId, setup.AssessmentId);

        using var opportunityResponse = await client.PostAsJsonAsync(
            $"/api/diagnoses/{setup.DiagnosisId}/opportunities",
            new
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
                phaseId = (Guid?)null,
                bottleneckIds = new[] { bottleneckId }
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, opportunityResponse.StatusCode);

        using var deleteResponse = await client.DeleteAsync(BottleneckUrl(setup.DiagnosisId, bottleneckId));

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    private async Task<HttpClient> CreateApiClientAsync()
    {
        await factory.ApplyMigrationsAsync();
        return factory.CreateClient();
    }

    private static async Task<(Guid DiagnosisId, Guid AssessmentId)> CreateDiagnosisWithAssessmentAsync(HttpClient client)
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
        return (diagnosisId, assessment.GetProperty("id").GetGuid());
    }

    private static async Task<Guid> CreateBottleneckAsync(HttpClient client, Guid diagnosisId, Guid assessmentId)
    {
        using var response = await client.PostAsJsonAsync(
            BottlenecksUrl(diagnosisId),
            BottleneckInput(assessmentId),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        return created.GetProperty("id").GetGuid();
    }

    private static object BottleneckInput(
        Guid sectorAssessmentId,
        string? title = "Conferência manual",
        string? category = "ManualWork",
        int impact = 3,
        decimal? estimatedMonthlyHours = null) => new
    {
        sectorAssessmentId,
        title,
        description = "A equipe confere os dados manualmente.",
        category,
        impact,
        frequency = "Diária",
        evidence = "Amostra de conferências",
        probableCause = "Sistemas separados",
        estimatedMonthlyHours,
        notes = (string?)null
    };

    private static string BottlenecksUrl(Guid diagnosisId) => $"/api/diagnoses/{diagnosisId}/bottlenecks";

    private static string BottleneckUrl(Guid diagnosisId, Guid bottleneckId) =>
        $"{BottlenecksUrl(diagnosisId)}/{bottleneckId}";

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private static void AssertOwnedResourceConflictOrNotFound(HttpStatusCode statusCode) =>
        Assert.Contains(statusCode, new[] { HttpStatusCode.Conflict, HttpStatusCode.NotFound });
}
