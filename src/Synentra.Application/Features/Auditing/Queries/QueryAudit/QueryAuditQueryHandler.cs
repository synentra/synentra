using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Domain.Common.Results;

namespace Synentra.Application.Features.Auditing.Queries.QueryAudit;

public sealed class QueryAuditQueryHandler : IActionHandler<QueryAuditQuery, PaginatedResult<AuditEntry>>
{
    private readonly IAuditStore _auditRepository;
    public QueryAuditQueryHandler(IAuditStore auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<PaginatedResult<AuditEntry>> Handle(QueryAuditQuery request, CancellationToken cancellationToken = default)
    {
        var page = await _auditRepository.QueryAsync(
            request.Filter,
            request.PageRequest,
            cancellationToken);

        return await PaginatedResult<AuditEntry>.SuccessAsync(page.Items, request.PageRequest, page.TotalCount);
    }
}
