using Synentra.Infrastructure.Configuration.DataProtection;
using System.Security.Cryptography.X509Certificates;
namespace Synentra.Infrastructure.Features.Secrets;

public static class KeyRingCertificates
{
    public static X509Certificate2 LoadActive(DataProtectionConfiguration options) 
        => Load(options.Certificate);

    private static X509Certificate2 Load(KeyRingCertificateConfiguration reference)
    {
        bool hasPath = !string.IsNullOrWhiteSpace(reference.Path);
        bool hasThumbprint = !string.IsNullOrWhiteSpace(reference.Thumbprint);
        if (hasPath == hasThumbprint)
            throw new InvalidOperationException("Specify exactly one certificate path or thumbprint.");

        X509Certificate2 certificate;
        if (hasPath)
        {
            var path = KeyRingSecurity.ResolvePath(reference.Path!);
            KeyRingSecurity.ValidateSecretFile(path);
            certificate = X509CertificateLoader.LoadPkcs12FromFile(path, reference.Password,
                X509KeyStorageFlags.EphemeralKeySet);
        }
        else
        {
            // Certificate stores and private-key access must be provisioned for the service account.
            var thumbprint = NormalizeThumbprint(reference.Thumbprint!);
            using var store = new X509Store(reference.StoreName, reference.StoreLocation);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            var matches = new List<X509Certificate2>();
            foreach (var candidate in store.Certificates)
            {
                if (NormalizeThumbprint(candidate.Thumbprint) == thumbprint) matches.Add(candidate);
                else candidate.Dispose();
            }
            if (matches.Count != 1)
            {
                foreach (var match in matches) match.Dispose();
                throw new InvalidOperationException("Certificate thumbprint must resolve to exactly one certificate.");
            }
            certificate = matches[0];
        }

        try
        {
            if (!certificate.HasPrivateKey)
                throw new InvalidOperationException("Key-ring certificates require private keys.");
            using var rsa = certificate.GetRSAPrivateKey();
            if (rsa is null)
                throw new InvalidOperationException("This key-ring implementation requires RSA certificates.");
            if (rsa.KeySize < 2048)
                throw new InvalidOperationException("RSA key-ring certificates must have at least 2048-bit keys.");
            var now = DateTime.UtcNow;
            if (now < certificate.NotBefore.ToUniversalTime() || now > certificate.NotAfter.ToUniversalTime())
                throw new InvalidOperationException("Active key-ring certificate is not currently valid.");
            return certificate;
        }
        catch
        {
            certificate.Dispose();
            throw;
        }
    }

    private static string NormalizeThumbprint(string value)
    {
        var normalized = string.Concat(value.Where(c => !char.IsWhiteSpace(c) && c != ':')).ToUpperInvariant();
        if (normalized.Length != 40 || normalized.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidOperationException("Certificate thumbprint must be a 40-digit hexadecimal SHA-1 identifier.");
        // SHA-1 is used here only as the certificate store identifier.
        return normalized;
    }
}

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