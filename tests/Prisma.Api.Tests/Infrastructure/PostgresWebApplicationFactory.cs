using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Prisma.Api.Data;

namespace Prisma.Api.Tests.Infrastructure;

public sealed class PostgresWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _testConnectionString = GetTestConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Prisma"] = _testConnectionString
            });
        });
    }

    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private static string GetTestConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PrismaTest");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings__PrismaTest for the dedicated PostgreSQL test database.");
        }

        var parsedConnectionString = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.Equals(parsedConnectionString.Database, "PrismaTest", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The test connection must target the dedicated PrismaTest database.");
        }

        // Minimal hosting reads environment configuration before ConfigureWebHost runs.
        // Set the validated test-only value early so Program cannot bind the normal database.
        Environment.SetEnvironmentVariable("ConnectionStrings__Prisma", connectionString);
        return connectionString;
    }
}
