using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Common.Security;

public interface ICredentialProvider
{
    string ProviderId { get; }
    Task<ResolvedCredential> ResolveAsync(CredentialBinding binding, CancellationToken ct);
}
