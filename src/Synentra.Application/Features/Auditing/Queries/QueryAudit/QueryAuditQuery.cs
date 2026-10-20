using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Domain.Common.Results;

namespace Synentra.Application.Features.Auditing.Queries.QueryAudit;

public sealed record QueryAuditQuery(AuditQuery Filter, PageRequest PageRequest) : IQuery<PaginatedResult<AuditEntry>>;
