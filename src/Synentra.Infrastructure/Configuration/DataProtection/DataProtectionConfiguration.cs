using System.Security.Cryptography.X509Certificates;

namespace Synentra.Infrastructure.Configuration.DataProtection;

public sealed class DataProtectionConfiguration : ISynentraConfiguration
{
    public static string SectionName => "DataProtection";

    public string ApplicationName { get; set; } = "synentra";
    public string KeyRingPath { get; set; } = "./data/keys";
    public bool RequireEncryptedKeyRing { get; set; } = true;
    public TimeSpan DefaultKeyLifetime { get; set; } = TimeSpan.FromDays(90);
    public KeyRingCertificateConfiguration Certificate { get; set; } = new();
}

public sealed class KeyRingCertificateConfiguration
{
    public string? Path { get; set; }
    public string? Password { get; set; }
    public string? Thumbprint { get; set; }
    public StoreName StoreName { get; set; } = StoreName.My;
    public StoreLocation StoreLocation { get; set; } = StoreLocation.CurrentUser;
}