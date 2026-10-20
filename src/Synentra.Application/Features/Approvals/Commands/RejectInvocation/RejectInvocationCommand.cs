using Synentra.Application.Common.Dispatching;
using Synentra.Domain.Security.ValueObjects;
using Unit = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Features.Approvals.Commands.RejectInvocation;

public sealed record RejectInvocationCommand(
    string ApprovalId, 
    CanonicalPrincipal Reviewer, 
    string? Reason) : ICommand<Unit>;
