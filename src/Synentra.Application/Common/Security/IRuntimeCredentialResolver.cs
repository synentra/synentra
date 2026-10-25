using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Common.Security;

public interface IRuntimeCredentialResolver
{
    Task<ResolvedCredential> ResolveAsync(CredentialBinding binding, ExecutionIdentity identity,
        string operationId, string targetId, CancellationToken ct);
}
