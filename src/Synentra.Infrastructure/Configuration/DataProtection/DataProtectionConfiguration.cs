using Synentra.Infrastructure.Features.Secrets;

namespace Synentra.Infrastructure.Configuration.DataProtection;

public class DataProtectionConfiguration : ISynentraConfiguration
{
    public static string SectionName => "DataProtection";

    public string ApplicationName { get; set; } = "synentra";
    public string KeyRingPath { get; set; } = "./data/keys";
    public Dictionary<string, AuthenticatedResponsePolicy> AuthenticatedResponsePolicies { get; set; } = new(StringComparer.Ordinal);
    public string? KeyRingCertificateThumbprint { get; set; }
    public string KeyRingCertificateStoreLocation { get; set; } = "LocalMachine";
    public string[] KeyRingPreviousCertificateThumbprints { get; set; } = [];
    public string? KeyRingCertificatePath { get; set; }
    public string? KeyRingCertificatePassword { get; set; }
    public bool RequireEncryptedKeyRing { get; set; } = true;
    public string[] KeyRingPreviousCertificatePaths { get; set; } = [];
    public TimeSpan? DefaultKeyLifetime { get; set; } = TimeSpan.FromDays(90);
}