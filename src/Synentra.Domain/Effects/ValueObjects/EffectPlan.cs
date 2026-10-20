using Synentra.Domain.Operations;
using Synentra.Domain.Resources.ValueObjects;

namespace Synentra.Domain.Effects.ValueObjects;

public sealed record EffectPlan
{
    public IReadOnlyList<ResourceDescriptor> AffectedResources { get; }
    public Mutability Mutability { get; }
    public IReadOnlyList<EffectQuantity> Quantities { get; }
    public IReadOnlyDictionary<string, string> Preconditions { get; }
    public string ResolverVersion { get; }
    public double Confidence { get; }
    public bool HasUnknownUpperBound => Quantities.Any(q => !q.IsBounded);

    public EffectPlan(
        IReadOnlyList<ResourceDescriptor> affectedResources, 
        Mutability mutability, IReadOnlyList<EffectQuantity> quantities, 
        IReadOnlyDictionary<string, string> preconditions, 
        string resolverVersion, double confidence)
    {
        AffectedResources = affectedResources;
        Mutability = mutability;
        Quantities = quantities;
        Preconditions = preconditions;
        ResolverVersion = resolverVersion;
        Confidence = confidence;
    }

    public static EffectPlan Empty(string resolverVersion) 
        => new(Array.Empty<ResourceDescriptor>(), Mutability.ReadOnly, Array.Empty<EffectQuantity>(), new Dictionary<string, string>(), resolverVersion, 1.0);
}
