using Prisma.Api.Features.Bottlenecks;

namespace Prisma.Api.Features.Opportunities;

public sealed class OpportunityBottleneck
{
    public Guid DiagnosisId { get; set; }

    public Guid OpportunityId { get; set; }

    public Guid BottleneckId { get; set; }

    public Opportunity Opportunity { get; set; } = null!;

    public Bottleneck Bottleneck { get; set; } = null!;
}
