using Synentra.Domain.Effects.ValueObjects;
using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Domain.Governance.Risk.ValueObjects;
using Synentra.Domain.Governance.Trust.ValueObjects;
using Synentra.Domain.Resources.ValueObjects;

namespace Synentra.Domain.Governance.Evidence;

public sealed record GovernanceEvidence
{
    public string ManifestRevision { get; init; } = "";
    public IntentLabel? Intent { get; init; }
    public RiskAssessment? Risk { get; init; }
    public TrustSnapshot? Trust { get; init; }
    public IReadOnlyList<ResourceDescriptor> Resources { get; init; }
    public EffectPlan? Effect { get; init; }
    public DateTimeOffset CollectedAt { get; init; }

    public GovernanceEvidence()
    {
        Resources = Array.Empty<ResourceDescriptor>();
        CollectedAt = DateTimeOffset.UtcNow;
    }
}
