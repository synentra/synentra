using Synentra.Infrastructure.Configuration.DataProtection;
using System.Security.Cryptography.X509Certificates;
namespace Synentra.Infrastructure.Features.Secrets;

public static class KeyRingCertificates
{
    public static X509Certificate2 LoadActive(DataProtectionConfiguration options)
    {
        if (options.KeyRingCertificateThumbprint is not null && options.KeyRingCertificatePath is not null)
            throw new InvalidOperationException("Choose one active certificate source.");
        var cert=options.KeyRingCertificateThumbprint is { } thumbprint ? FromStore(thumbprint,options.KeyRingCertificateStoreLocation) : FromFile(options.KeyRingCertificatePath!,options.KeyRingCertificatePassword);
        using var privateKey = cert.GetRSAPrivateKey();
        if (!cert.HasPrivateKey || privateKey is not { KeySize: >=2048 } || cert.NotAfter.ToUniversalTime() <= DateTime.UtcNow || cert.NotBefore.ToUniversalTime() > DateTime.UtcNow)
            throw new InvalidOperationException("Active key-ring certificate requires a valid RSA private key of at least 2048 bits.");
        return cert;
    }

    public static X509Certificate2[] LoadPrevious(DataProtectionConfiguration options) 
        => options.KeyRingPreviousCertificatePaths
        .Select(path=>FromFile(path,options.KeyRingCertificatePassword))
        .Concat(options.KeyRingPreviousCertificateThumbprints.Select(t=>FromStore(t,options.KeyRingCertificateStoreLocation)))
        .ToArray();

    private static X509Certificate2 FromFile(string path,string? password)
    {
        KeyRingSecurity.ValidateSecretFile(path);
        return X509CertificateLoader.LoadPkcs12FromFile(path,password,X509KeyStorageFlags.EphemeralKeySet);
    }

    private static X509Certificate2 FromStore(string thumbprint,string location)
    {
        if (!OperatingSystem.IsWindows() || location is not ("CurrentUser" or "LocalMachine")) 
            throw new InvalidOperationException("Invalid Windows certificate-store configuration.");

        using var store=new X509Store(StoreName.My,Enum.Parse<StoreLocation>(location)); 
        store.Open(OpenFlags.ReadOnly);
        var matches=store.Certificates.Find(X509FindType.FindByThumbprint,thumbprint,validOnly:false);
        if (matches.Count!=1 || !matches[0].HasPrivateKey) 
            throw new InvalidOperationException("Key-ring certificate not found or inaccessible.");

        return matches[0];
    }
}

