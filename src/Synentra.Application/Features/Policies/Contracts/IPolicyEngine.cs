namespace Synentra.Application.Features.Policies.Contracts;

public interface IPolicyEngine
{
    string EngineId { get; }
    string Version { get; }
    Task<PolicyResult> EvaluateAsync(PolicyInput input, CancellationToken ct);
}
