using Synentra.Domain.Common.Results;

namespace Synentra.Application.Features.Auditing.Contracts;

public interface IAuditStore
{
    Task AppendAsync(AuditEntry entry, CancellationToken ct);
    Task<Page<AuditEntry>> QueryAsync(AuditQuery query, PageRequest pageRequest, CancellationToken ct);
}
