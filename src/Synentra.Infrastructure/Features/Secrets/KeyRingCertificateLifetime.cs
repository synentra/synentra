using System.Security.Cryptography.X509Certificates;

namespace Synentra.Infrastructure.Features.Secrets;

internal sealed class KeyRingCertificateLifetime : IDisposable
{
    private readonly X509Certificate2? certificate;
    private bool disposed;
    public KeyRingCertificateLifetime(X509Certificate2? certificate) => this.certificate = certificate;
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        certificate?.Dispose();
    }
}