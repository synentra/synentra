using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Auditing.Queries.QueryAudit;
using Synentra.Domain.Common.Results;

namespace Synentra.Application.Features.Auditing;

public static class AuditingDispatcherExtensions
{
    public static async Task<PaginatedResult<AuditEntry>> QueryAudits(
        this IDispatcher dispatcher,
        AuditQuery filter,
        int? pageNumber, 
        int? pageSize,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new QueryAuditQuery(filter, new PageRequest(pageNumber, pageSize)), cancellationToken);
    }
}
