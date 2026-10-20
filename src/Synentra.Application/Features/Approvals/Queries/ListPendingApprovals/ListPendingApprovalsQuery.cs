using Synentra.Application.Common.Dispatching;
using Synentra.Domain.Approvals;
using Synentra.Domain.Common.Results;

namespace Synentra.Application.Features.Approvals.Queries.ListPendingApprovals;

public sealed record ListPendingApprovalsQuery(
    string SecurityScope, 
    PageRequest PageRequest) : IQuery<PaginatedResult<Approval>>;
