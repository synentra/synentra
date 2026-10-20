using Synentra.Application.Common.Dispatching;
using Synentra.Domain.Security.ValueObjects;
using Unit = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Features.Approvals.Commands.ApproveInvocation;

public sealed record ApproveInvocationCommand(
    string ApprovalId, 
    CanonicalPrincipal Reviewer, 
    bool AllowsSelfApproval) : ICommand<Unit>;
