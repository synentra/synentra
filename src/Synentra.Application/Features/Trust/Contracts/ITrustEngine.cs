using Synentra.Domain.Governance.Trust.ValueObjects;

namespace Synentra.Application.Features.Trust.Contracts;

public interface ITrustEngine
{
    string ProfileId { get; }
    Task<TrustSnapshot> LoadSnapshotAsync(string agentId, string securityScope, CancellationToken ct);
    Task RecordEvidenceAsync(TrustEvidence evidence, CancellationToken ct);
}
