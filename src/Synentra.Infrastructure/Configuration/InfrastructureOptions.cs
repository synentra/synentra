namespace Synentra.Infrastructure.Configuration;

public sealed class InfrastructureOptions
{
    public string KeyRingPath { get; set; } = "";
    public string PolicyProvider { get; set; } = "declarative";
    public string? OpaEndpoint { get; set; }
    public string OpaPolicyPath { get; set; } = "synentra/authz/decision";
    public bool AllowSelfApproval { get; set; }
    public bool AllowCrossScopeApproval { get; set; }
    public double MaxManualTrustAdjustment { get; set; } = 10;
    public double InitialTrustScore { get; set; } = 50;
    public double QuarantineThreshold { get; set; } = 10;
    public Dictionary<string, double> EvidenceWeights { get; set; } = new()
    {
        ["SuccessfulExecution"] = 1,
        ["ConfirmedMisuse"] = -25,
        ["PolicyViolation"] = -5,
        ["ReviewerAction"] = 0
    };
    public HashSet<string> AllowedOrigins { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool AllowPrivateAddresses { get; set; }
    public int MaximumResponseBytes { get; set; } = 2 * 1024 * 1024;
    public int HttpTimeoutSeconds { get; set; } = 30;
    public string? IntentEndpoint { get; set; }
    public string IntentProviderId { get; set; } = "semantic-http";
    public bool StartOutboxWorker { get; set; } = true;
}
