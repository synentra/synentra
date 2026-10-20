using Synentra.Domain.Invocations.ValueObjects;

namespace Synentra.Domain.Approvals.Events;

public sealed record ApprovalRequested(
    InvocationId InvocationId, 
    string ApprovalId, 
    BindingDigest BindingDigest, 
    DateTimeOffset ExpiresAt);