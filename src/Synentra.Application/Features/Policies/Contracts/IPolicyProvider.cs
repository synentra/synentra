namespace Synentra.Application.Features.Policies.Contracts;

public interface IPolicyProvider
{
    string ProviderId { get; }
    string Version { get; }
    Task<PolicyResult> EvaluateAsync(PolicyInput input, CancellationToken ct);
}
