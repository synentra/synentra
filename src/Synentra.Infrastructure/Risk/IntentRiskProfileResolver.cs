using Microsoft.Extensions.Options;
using Synentra.BuildingBlocks.Configuration.Risk;

namespace Synentra.Infrastructure.Risk;

public sealed class IntentRiskProfileResolver
{
    private readonly double _defaultUnknownIntentScore;
    private readonly string[] _defaultUnknownIntentTags;
    private readonly Dictionary<string, IntentRiskProfileConfiguration> _profiles;

    public IntentRiskProfileResolver(IOptions<RiskConfiguration> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var risk = configuration.Value ?? new RiskConfiguration();

        _defaultUnknownIntentScore = Math.Clamp(risk.DefaultUnknownIntentScore, 0.0, 1.0);
        _defaultUnknownIntentTags = risk.DefaultUnknownIntentTags ?? [];

        _profiles = new Dictionary<string, IntentRiskProfileConfiguration>(StringComparer.OrdinalIgnoreCase);

        if (risk.IntentProfiles is null)
            return;

        foreach (var (key, value) in risk.IntentProfiles)
        {
            if (string.IsNullOrWhiteSpace(key) || value is null)
                continue;

            _profiles[key.Trim()] = new IntentRiskProfileConfiguration
            {
                BaseRiskScore = Math.Clamp(value.BaseRiskScore, 0.0, 1.0),
                RiskTags = value.RiskTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToArray() ?? []
            };
        }
    }

    public double ResolveScore(string? label)
    {
        if (TryResolveProfile(label, out var profile))
            return profile.BaseRiskScore;

        return _defaultUnknownIntentScore;
    }

    public string[] ResolveRiskTags(string? label)
    {
        if (TryResolveProfile(label, out var profile))
            return profile.RiskTags;

        return _defaultUnknownIntentTags;
    }

    private bool TryResolveProfile(string? label, out IntentRiskProfileConfiguration profile)
    {
        if (!string.IsNullOrWhiteSpace(label) && _profiles.TryGetValue(label.Trim(), out profile!))
            return true;

        profile = default!;
        return false;
    }
}
