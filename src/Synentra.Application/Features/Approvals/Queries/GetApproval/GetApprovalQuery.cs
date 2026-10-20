using Synentra.Domain.Approvals;
using Synentra.Application.Common.Dispatching;

namespace Synentra.Application.Features.Approvals.Queries.GetApproval;

public sealed record GetApprovalQuery(string ApprovalId) : IQuery<Approval?>;
