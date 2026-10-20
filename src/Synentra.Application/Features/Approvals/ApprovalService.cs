using Synentra.Domain.Approvals;
using Synentra.Domain.Invocations;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Approvals.Commands.ApproveInvocation;
using Synentra.Application.Features.Approvals.Commands.CreateApproval;
using Synentra.Application.Features.Approvals.Commands.RejectInvocation;
using Synentra.Application.Features.Approvals.Commands.RevokeApproval;

namespace Synentra.Application.Features.Approvals;

public sealed class ApprovalService
{
    private readonly IDispatcher _dispatcher;

    public ApprovalService(IDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task<Approval> CreateAsync(
        ToolInvocation invocation, 
        string policyRevision, 
        string reviewObligationRevision, 
        CancellationToken ct) 
        => _dispatcher.Dispatch(new CreateApprovalCommand(invocation, policyRevision, reviewObligationRevision), ct);

    public Task ApproveAsync(
        string approvalId, 
        CanonicalPrincipal reviewer, 
        bool allowsSelfApproval, 
        CancellationToken ct) 
        => _dispatcher.Dispatch(new ApproveInvocationCommand(approvalId, reviewer, allowsSelfApproval), ct);

    public Task RejectAsync(
        string approvalId, 
        CanonicalPrincipal reviewer, 
        string? reason, 
        CancellationToken ct) 
        => _dispatcher.Dispatch(new RejectInvocationCommand(approvalId, reviewer, reason), ct);

    public Task RevokeAsync(
        string approvalId, 
        CanonicalPrincipal by, 
        CancellationToken ct) 
        => _dispatcher.Dispatch(new RevokeApprovalCommand(approvalId, by), ct);
}
