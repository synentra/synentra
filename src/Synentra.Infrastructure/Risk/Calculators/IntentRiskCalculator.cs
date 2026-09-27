using Microsoft.Extensions.Options;
using Synentra.Application.Models;
using Synentra.BuildingBlocks.Configuration.Risk;

namespace Synentra.Infrastructure.Risk.Calculators;

public sealed class IntentRiskCalculator : IRiskCalculator
{
    private readonly IntentRiskProfileResolver _intentProfiles;

    public IntentRiskCalculator()
        : this(new IntentRiskProfileResolver(Options.Create(new RiskConfiguration())))
    {
    }

    public IntentRiskCalculator(IntentRiskProfileResolver intentProfiles)
    {
        _intentProfiles = intentProfiles ?? throw new ArgumentNullException(nameof(intentProfiles));
    }

    public string Name => "IntentRisk";
    public double Weight { get; } = 0.25;

    public Task<RiskCalculatorResult> CalculateAsync(RiskEvaluationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var intent = context.Intent;

        var baseScore = _intentProfiles.ResolveScore(intent.Label);

        var confidenceAdjustment = intent.Status switch
        {
            IntentClassificationStatus.Classified => 0.0,
            IntentClassificationStatus.LowConfidence => 0.10,
            IntentClassificationStatus.Failed => 0.15,
            IntentClassificationStatus.Unavailable => 0.15,
            _ => 0.10
        };

        var score = Math.Clamp(baseScore + confidenceAdjustment, 0.0, 1.0);

        var signal = new RiskSignal
        {
            Code = "intent_risk",
            Description = $"Intent '{intent.Label}' with confidence {intent.Confidence:F2}."
        };

        return Task.FromResult(RiskCalculatorResult.Create(Name, score, Weight, new[] { signal }));
    }
}
