using Synentra.Application.Features.Risk.Contracts;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Risk;
using Synentra.Domain.Governance.Risk.ValueObjects;

namespace Synentra.Infrastructure.Features.Risk;

public sealed class TrustRiskFactor : IRiskFactorPlugin
{
    public string Name => "trust";
    public IntentModality SupportedModalities => (IntentModality)(-1);

    public Task<RiskFactorResult> EvaluateAsync(RiskInput input, CancellationToken ct) 
        => Task.FromResult(
            new RiskFactorResult(
                Name, 
                Math.Clamp(1 - input.Trust.Score.Value / 100, 0, 1), 
                1, 
                "Inverse normalized trust score; configure additional domain factors for production.", 
                RiskFactorStatus.Applied));
}
