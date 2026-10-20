namespace Synentra.Application.Features.Invocations.Contracts;

public interface IIdempotencyStore
{
    Task<IdempotencyEntry?> GetAsync(
        string securityScope, 
        string actorId, 
        string operationId, 
        string key, 
        CancellationToken ct);

    Task<IdempotencyEntry> RecordAsync(
        string securityScope, 
        string actorId, 
        string operationId, 
        string key, 
        string canonicalInputDigest, 
        string exactBindingDigest, 
        string invocationId, 
        DateTimeOffset expiresAt, 
        CancellationToken ct);
}
