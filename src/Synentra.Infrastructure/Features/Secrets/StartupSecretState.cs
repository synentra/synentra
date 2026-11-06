using Synentra.Infrastructure.Features.Identity;
using Synentra.Infrastructure.Configuration.Identity;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class StartupSecretState
{
    private sealed record Snapshot(
        IdentityConfiguration IdentityConfiguration, 
        DateTimeOffset? ExpiresAt, 
        OidcPrincipalResolver? Oidc);

    private Snapshot? snapshot;

    public bool IsReady => Volatile.Read(ref snapshot) is { } value && (value.ExpiresAt is null || value.ExpiresAt > DateTimeOffset.UtcNow);

    public IdentityConfiguration Identity => IsReady ? snapshot!.IdentityConfiguration : 
        throw new InvalidOperationException("Startup secrets are not ready or have expired.");

    public OidcPrincipalResolver Oidc => IsReady && snapshot!.Oidc is { } resolver 
        ? resolver 
        : throw new InvalidOperationException("OIDC was not initialized.");

    public void Publish(IdentityConfiguration identityConfiguration, DateTimeOffset? expiresAt, OidcPrincipalResolver? oidc = null)
    {
        if (Interlocked.CompareExchange(ref snapshot, new(identityConfiguration, expiresAt, oidc), null) is not null) 
            throw new InvalidOperationException("Startup secrets already initialized.");
    }

    public override string ToString() => "StartupSecretState([REDACTED])";
}