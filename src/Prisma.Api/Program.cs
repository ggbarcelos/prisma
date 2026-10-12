using Npgsql;
using Microsoft.EntityFrameworkCore;
using Prisma.Api.Data;
using Prisma.Api.Features.Clients;
using Prisma.Api.Features.Bottlenecks;
using Prisma.Api.Features.Diagnoses;
using Prisma.Api.Features.SectorAssessments;
using Prisma.Api.Features.Opportunities;
using Prisma.Api.Features.Phases;
using Prisma.Api.Features.Reports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Prisma");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure ConnectionStrings:Prisma with User Secrets or an environment variable.");
}

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<NpgsqlDataSource>()));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length == 0 || allowedOrigins.Any(origin =>
        !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
        string.IsNullOrWhiteSpace(uri.Host)))
{
    throw new InvalidOperationException(
        "Configure Cors:AllowedOrigins with one or more explicit HTTP(S) client origins.");
}

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseCors();

app.MapClientEndpoints();
app.MapClientSectorEndpoints();
app.MapDiagnosisEndpoints();
app.MapSectorAssessmentEndpoints();
app.MapBottleneckEndpoints();
app.MapOpportunityEndpoints();
app.MapPhaseEndpoints();
app.MapReportEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapFallback("/api/{*path}", () => Results.Problem(
    statusCode: StatusCodes.Status404NotFound,
    title: "Endpoint não encontrado."));
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
