using Synentra.Application.Common.Dispatching;
using Synentra.Domain.Security.ValueObjects;
using Unit = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Features.Approvals.Commands.RevokeApproval;

public sealed record RevokeApprovalCommand(string ApprovalId, CanonicalPrincipal By) : ICommand<Unit>;
