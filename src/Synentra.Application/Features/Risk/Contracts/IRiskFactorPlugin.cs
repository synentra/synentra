using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Risk.ValueObjects;

namespace Synentra.Application.Features.Risk.Contracts;

public interface IRiskFactorPlugin
{
    string Name { get; }
    IntentModality SupportedModalities { get; }
    Task<RiskFactorResult> EvaluateAsync(RiskInput input, CancellationToken ct);
}
