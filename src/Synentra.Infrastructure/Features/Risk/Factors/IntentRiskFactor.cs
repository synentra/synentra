using Synentra.Application.Features.Risk.Contracts;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Risk;
using Synentra.Domain.Governance.Risk.ValueObjects;

namespace Synentra.Infrastructure.Features.Risk.Factors;

public sealed class IntentRiskFactor : IRiskFactorPlugin
{
    public string Name => "risk-factor/intent";
    public IntentModality SupportedModalities => (IntentModality)(-1);

    public Task<RiskFactorResult> EvaluateAsync(RiskInput input, CancellationToken ct)
    {
        if (input.Intent is null)
            return Task.FromResult(new RiskFactorResult(Name, 0.15, 0.15, "No intent classification was available.", RiskFactorStatus.Applied));

        var risk = input.Intent.Status switch
        {
            IntentStatus.Available => input.Intent.Value.Contains("read", StringComparison.OrdinalIgnoreCase) ? 0.1 : 0.35,
            IntentStatus.LowConfidence => 0.5,
            IntentStatus.Unsupported or IntentStatus.Timeout or IntentStatus.Error => 0.65,
            _ => 0.5
        } + (1 - input.Intent.Confidence) * 0.15;
        return Task.FromResult(new RiskFactorResult(Name, Math.Clamp(risk, 0, 1), 0.15, $"Intent '{input.Intent.Value}' confidence {input.Intent.Confidence:F2}.", RiskFactorStatus.Applied));
    }
}