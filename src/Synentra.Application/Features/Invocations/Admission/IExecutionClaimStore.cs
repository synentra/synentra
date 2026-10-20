namespace Synentra.Application.Features.Invocations.Admission;

public interface IExecutionClaimStore
{
    Task<string?> TryAcquireAsync(string invocationId, string claimedBy, TimeSpan ttl, CancellationToken ct);
    Task<bool> TransitionToDispatchingAsync(string claimId, CancellationToken ct);
    Task MarkRespondedAsync(string claimId, CancellationToken ct);
    Task<bool> CancelBeforeDispatchAsync(string invocationId, CancellationToken ct);
}
