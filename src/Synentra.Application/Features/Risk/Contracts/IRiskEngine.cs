using Synentra.Domain.Governance.Risk.ValueObjects;

namespace Synentra.Application.Features.Risk.Contracts;

public interface IRiskEngine
{
    string CalculatorId { get; }
    string Version { get; }
    Task<RiskAssessment> EvaluateAsync(RiskInput input, CancellationToken ct);
}
