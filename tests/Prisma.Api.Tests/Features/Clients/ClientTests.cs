using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prisma.Api.Tests.Infrastructure;

namespace Prisma.Api.Tests.Features.Clients;

public sealed class ClientTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateClient_PersistsRequiredFieldsAndAcceptsEmptyOptionalFields()
    {
        using var client = await CreateApiClientAsync();
        var token = NewToken();
        var input = ClientInput($"Empresa {token}", "Tecnologia", null, null, null, null);

        using var response = await client.PostAsJsonAsync("/api/clients", input, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        var clientId = created.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, clientId);
        Assert.Equal($"Empresa {token}", created.GetProperty("companyName").GetString());
        Assert.Equal("Tecnologia", created.GetProperty("segment").GetString());
        Assert.Equal(JsonValueKind.Null, created.GetProperty("contactName").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.GetProperty("email").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.GetProperty("phone").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.GetProperty("notes").ValueKind);

        using var reload = await client.GetAsync($"/api/clients/{clientId}");
        Assert.Equal(HttpStatusCode.OK, reload.StatusCode);
        var persisted = await ReadJsonAsync(reload);
        Assert.Equal($"Empresa {token}", persisted.GetProperty("companyName").GetString());
    }

    [Theory]
    [InlineData("", "Tecnologia")]
    [InlineData("Empresa", "")]
    public async Task CreateClient_RejectsMissingCompanyNameOrSegment(string companyName, string segment)
    {
        using var client = await CreateApiClientAsync();
        var input = ClientInput(companyName, segment, null, null, null, null);

        using var response = await client.PostAsJsonAsync("/api/clients", input, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchClients_MatchesCompanySegmentAndContactName()
    {
        using var client = await CreateApiClientAsync();
        var token = NewToken();
        var expectedIds = new[]
        {
            await CreateClientAsync(client, $"Empresa-{token}", "Varejo", null),
            await CreateClientAsync(client, $"Outra-{token}", $"Segmento-{token}", null),
            await CreateClientAsync(client, $"Terceira-{token}", "Serviços", $"Contato-{token}")
        };

        using var response = await client.GetAsync($"/api/clients?search={Uri.EscapeDataString(token)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await ReadJsonAsync(response);
        var actualIds = results.EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToHashSet();
        Assert.All(expectedIds, id => Assert.Contains(id, actualIds));
    }

    [Fact]
    public async Task UpdateClient_PersistsProfileChanges()
    {
        using var client = await CreateApiClientAsync();
        var token = NewToken();
        var clientId = await CreateClientAsync(client, $"Antes-{token}", "Varejo", "Ana");
        var update = ClientInput($"Depois-{token}", "Tecnologia", "Bruno", "bruno@example.test", "+55 11 99999-0000", "Atualizado");

        using var response = await client.PutAsJsonAsync($"/api/clients/{clientId}", update, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await ReadJsonAsync(response);
        Assert.Equal($"Depois-{token}", updated.GetProperty("companyName").GetString());
        Assert.Equal("Tecnologia", updated.GetProperty("segment").GetString());
        Assert.Equal("Bruno", updated.GetProperty("contactName").GetString());
        Assert.Equal("bruno@example.test", updated.GetProperty("email").GetString());
        Assert.Equal("+55 11 99999-0000", updated.GetProperty("phone").GetString());
        Assert.Equal("Atualizado", updated.GetProperty("notes").GetString());
    }

    [Fact]
    public async Task ClientSectors_CanBeCreatedListedAndUpdatedWithoutDeleteOperations()
    {
        using var client = await CreateApiClientAsync();
        var token = NewToken();
        var clientId = await CreateClientAsync(client, $"Empresa-{token}", "Serviços", null);
        var firstSectorId = await CreateSectorAsync(client, clientId, $"Financeiro-{token}", null, null);
        var secondSectorId = await CreateSectorAsync(client, clientId, $"Operações-{token}", "Carla", 12);

        using var listResponse = await client.GetAsync($"/api/clients/{clientId}/sectors");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var sectors = await ReadJsonAsync(listResponse);
        var listedSectors = sectors.EnumerateArray().ToArray();
        Assert.Equal(2, listedSectors.Length);
        Assert.Contains(listedSectors, sector => sector.GetProperty("id").GetGuid() == firstSectorId);
        Assert.Contains(listedSectors, sector =>
            sector.GetProperty("id").GetGuid() == secondSectorId &&
            sector.GetProperty("responsibleName").GetString() == "Carla" &&
            sector.GetProperty("peopleCount").GetInt32() == 12);

        var update = SectorInput($"Controladoria-{token}", "Daniela", 4);
        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/clients/{clientId}/sectors/{firstSectorId}", update, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var clientDelete = await client.DeleteAsync($"/api/clients/{clientId}");
        using var sectorDelete = await client.DeleteAsync($"/api/clients/{clientId}/sectors/{secondSectorId}");
        Assert.False(clientDelete.IsSuccessStatusCode);
        Assert.False(sectorDelete.IsSuccessStatusCode);

        using var reloadResponse = await client.GetAsync($"/api/clients/{clientId}/sectors");
        Assert.Equal(HttpStatusCode.OK, reloadResponse.StatusCode);
        var reloadedSectors = await ReadJsonAsync(reloadResponse);
        Assert.Contains(reloadedSectors.EnumerateArray(), sector =>
            sector.GetProperty("id").GetGuid() == firstSectorId &&
            sector.GetProperty("name").GetString() == $"Controladoria-{token}" &&
            sector.GetProperty("responsibleName").GetString() == "Daniela" &&
            sector.GetProperty("peopleCount").GetInt32() == 4);
        Assert.Contains(reloadedSectors.EnumerateArray(), sector =>
            sector.GetProperty("id").GetGuid() == secondSectorId);
    }

    private async Task<HttpClient> CreateApiClientAsync()
    {
        await factory.ApplyMigrationsAsync();
        return factory.CreateClient();
    }

    private static async Task<Guid> CreateClientAsync(
        HttpClient client,
        string companyName,
        string segment,
        string? contactName)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/clients",
            ClientInput(companyName, segment, contactName, null, null, null),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        return created.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateSectorAsync(
        HttpClient client,
        Guid clientId,
        string name,
        string? responsibleName,
        int? peopleCount)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/clients/{clientId}/sectors",
            SectorInput(name, responsibleName, peopleCount),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        Assert.Equal(clientId, created.GetProperty("clientId").GetGuid());
        return created.GetProperty("id").GetGuid();
    }

    private static object ClientInput(
        string companyName,
        string segment,
        string? contactName,
        string? email,
        string? phone,
        string? notes) => new
        {
            companyName,
            segment,
            contactName,
            email,
            phone,
            notes
        };

    private static object SectorInput(string name, string? responsibleName, int? peopleCount) => new
    {
        name,
        responsibleName,
        peopleCount
    };

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private static string NewToken() => Guid.NewGuid().ToString("N");
}
