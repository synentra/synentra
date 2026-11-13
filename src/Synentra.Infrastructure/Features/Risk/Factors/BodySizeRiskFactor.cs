using System.Text.Json;
using Synentra.Application.Features.Risk.Contracts;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Risk;
using Synentra.Domain.Governance.Risk.ValueObjects;

namespace Synentra.Infrastructure.Features.Risk.Factors;

public sealed class BodySizeRiskFactor : IRiskFactorPlugin
{
    public string Name => "risk-factor/body-size";
    public IntentModality SupportedModalities => (IntentModality)(-1);

    public Task<RiskFactorResult> EvaluateAsync(RiskInput input, CancellationToken ct)
    {
        var size = JsonSerializer.SerializeToUtf8Bytes(input.Invocation.Arguments).Length;
        var risk = size > 1024 * 1024 ? 0.8 : size > 100 * 1024 ? 0.5 : size > 10 * 1024 ? 0.2 : 0;
        return Task.FromResult(new RiskFactorResult(Name, risk, 0.05, $"Serialized argument size is {size} bytes.", RiskFactorStatus.Applied));
    }
}