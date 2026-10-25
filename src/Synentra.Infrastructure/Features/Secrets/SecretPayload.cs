using Synentra.Application.Common.Security;
using Synentra.Infrastructure.Diagnostics;

namespace Synentra.Infrastructure.Features.Secrets;

public static class SecretPayload
{
    public static void Validate(ResolvedCredential credential)
    {
        if (string.IsNullOrWhiteSpace(credential.Value) 
            || credential.Value.Length > 65536 
            || credential.Value.Any(char.IsControl) 
            || credential.Scheme is not ("Bearer" or "Basic") 
            || credential.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new CredentialNotFoundException(InfrastructureErrors.Credential.CredentialUnavailableOrInvalid);
    }
}
