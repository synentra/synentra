using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class KeyRingStartupValidator : IHostedService
{
    private readonly IKeyManager keys;
    private readonly IDataProtectionProvider provider;
    private readonly PrivateKeyRingRepository repository;

    // The DI-created lifetime wrapper owns the active certificate until host disposal.
    internal KeyRingStartupValidator(IKeyManager keys, IDataProtectionProvider provider,
        PrivateKeyRingRepository repository, KeyRingCertificateLifetime certificateLifetime)
    {
        this.keys = keys;
        this.provider = provider;
        this.repository = repository;
        _ = certificateLifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = repository.GetAllElements();

        foreach (var key in keys.GetAllKeys())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!key.IsRevoked && key.CreateEncryptor() is null)
                throw new InvalidOperationException("An existing key cannot create an authenticated encryptor.");
        }

        // Also exercises default-key selection, first-run generation, persistence and use.
        var protector = provider.CreateProtector("Synentra.KeyRing.StartupProbe.v1");
        var plaintext = RandomNumberGenerator.GetBytes(32);
        byte[]? recovered = null;
        try
        {
            var protectedValue = protector.Protect(plaintext);
            recovered = protector.Unprotect(protectedValue);
            if (!CryptographicOperations.FixedTimeEquals(plaintext, recovered))
                throw new InvalidOperationException("Data Protection startup round trip failed.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            if (recovered is not null) CryptographicOperations.ZeroMemory(recovered);
        }
        _ = repository.GetAllElements();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
