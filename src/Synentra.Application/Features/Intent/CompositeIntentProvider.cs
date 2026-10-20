using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Domain.Governance.Intent;
using Synentra.Application.Features.Intent.Contracts;

namespace Synentra.Application.Features.Intent;

public sealed class CompositeIntentProvider : IIntentProvider
{
    private readonly IReadOnlyDictionary<IntentModality, IIntentProvider> _providers;

    public CompositeIntentProvider(IEnumerable<IIntentProvider> providers)
    {
        var map = new Dictionary<IntentModality, IIntentProvider>();
        foreach (var p in providers.Where(p => p is not CompositeIntentProvider))
        {
            foreach (var bit in Enum.GetValues<IntentModality>())
            {
                if (bit == IntentModality.None)
                    continue;
                if (p.SupportedModalities.HasFlag(bit) && !map.ContainsKey(bit))
                    map[bit] = p;
            }
        }

        _providers = map;
    }

    public string ProviderId => "composite";
    public string ContractVersion => "1.0.0";
    public IntentModality SupportedModalities => _providers.Keys.Aggregate(IntentModality.None, (a, b) => a | b);

    public async Task<IntentLabel> ClassifyAsync(IntentEvidence evidence, CancellationToken ct)
    {
        foreach (var bit in Enum.GetValues<IntentModality>())
        {
            if (bit == IntentModality.None)
                continue;
            if (!evidence.Modality.HasFlag(bit))
                continue;
            if (_providers.TryGetValue(bit, out var p))
                return await p.ClassifyAsync(evidence, ct);
        }

        // Unsupported modality must not receive fabricated confidence
        return new IntentLabel("unsupported", 0, IntentStatus.Unsupported, "composite");
    }
}
