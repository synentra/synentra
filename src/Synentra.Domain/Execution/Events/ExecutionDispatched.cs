using Synentra.Domain.Invocations.ValueObjects;

namespace Synentra.Domain.Execution.Events;

public sealed record ExecutionDispatched(
    InvocationId InvocationId, 
    string UpstreamReference, 
    DateTimeOffset DispatchedAt);
