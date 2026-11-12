using Synentra.Application.Features.Risk.Contracts;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Risk;
using Synentra.Domain.Governance.Risk.ValueObjects;

namespace Synentra.Infrastructure.Features.Risk.Factors;

public sealed class TimeBasedRiskFactor : IRiskFactorPlugin
{
    public string Name => "time";
    public IntentModality SupportedModalities => (IntentModality)(-1);

    public Task<RiskFactorResult> EvaluateAsync(RiskInput input, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var risk = now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0.2 : 0;
        risk += now.Hour < 6 || now.Hour > 20 ? 0.3 : now.Hour < 8 || now.Hour > 18 ? 0.1 : 0;
        return Task.FromResult(new RiskFactorResult(Name, Math.Min(0.5, risk), 0.05, "Risk based on UTC day and hour.", RiskFactorStatus.Applied));
    }
}