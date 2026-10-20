using Synentra.Application.Common.Dispatching;
using Synentra.Domain.Approvals;
using Synentra.Domain.Invocations;

namespace Synentra.Application.Features.Approvals.Commands.CreateApproval;

public sealed record CreateApprovalCommand(
    ToolInvocation Invocation, 
    string PolicyRevision, 
    string ReviewObligationRevision) : ICommand<Approval>;
