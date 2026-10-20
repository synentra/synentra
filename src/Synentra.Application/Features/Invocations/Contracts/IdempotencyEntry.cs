namespace Synentra.Application.Features.Invocations.Contracts;

public sealed record IdempotencyEntry(
    string Id, 
    string SecurityScope, 
    string ActorId, 
    string OperationId,
    string Key, 
    string CanonicalInputDigest,
    string ExactBindingDigest, 
    string InvocationId,
    DateTimeOffset ExpiresAt);
