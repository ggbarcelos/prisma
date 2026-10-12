using Microsoft.EntityFrameworkCore;
using Prisma.Api.Features.Bottlenecks;
using Prisma.Api.Features.Clients;
using Prisma.Api.Features.Diagnoses;
using Prisma.Api.Features.Opportunities;
using Prisma.Api.Features.Phases;
using Prisma.Api.Features.SectorAssessments;

namespace Prisma.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Prisma.Api.Features.Clients.Client> Clients => Set<Prisma.Api.Features.Clients.Client>();

    public DbSet<ClientSector> ClientSectors => Set<ClientSector>();

    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();

    public DbSet<DiagnosisObjective> DiagnosisObjectives => Set<DiagnosisObjective>();

    public DbSet<SectorAssessment> SectorAssessments => Set<SectorAssessment>();

    public DbSet<Bottleneck> Bottlenecks => Set<Bottleneck>();

    public DbSet<Opportunity> Opportunities => Set<Opportunity>();

    public DbSet<OpportunityBottleneck> OpportunityBottlenecks => Set<OpportunityBottleneck>();

    public DbSet<Phase> Phases => Set<Phase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
