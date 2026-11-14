using Synentra.Domain.Governance.Risk.ValueObjects;

namespace Synentra.Application.Features.Risk.Contracts;

public interface IRiskProvider
{
    string ProviderId { get; }
    string Version { get; }
    Task<RiskAssessment> EvaluateAsync(RiskInput input, CancellationToken ct);
}
