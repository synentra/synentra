namespace Synentra.BuildingBlocks.Configuration.Risk;

public sealed class IntentRiskProfileConfiguration
{
    public double BaseRiskScore { get; set; }
    public string[] RiskTags { get; set; } = [];

    public static Dictionary<string, IntentRiskProfileConfiguration> CreateDefaultProfiles()
    {
        return new Dictionary<string, IntentRiskProfileConfiguration>(StringComparer.OrdinalIgnoreCase)
        {
            ["health_check"] = new() { BaseRiskScore = 0.05 },
            ["safe_read"] = new() { BaseRiskScore = 0.10 },
            ["list"] = new() { BaseRiskScore = 0.10 },
            ["audit"] = new() { BaseRiskScore = 0.20 },
            ["create"] = new() { BaseRiskScore = 0.35 },
            ["safe_write"] = new() { BaseRiskScore = 0.40 },
            ["update"] = new() { BaseRiskScore = 0.40 },
            ["export"] = new() { BaseRiskScore = 0.60, RiskTags = ["data_exfiltration"] },
            ["configure"] = new() { BaseRiskScore = 0.60 },
            ["bulk_export"] = new() { BaseRiskScore = 0.75, RiskTags = ["data_exfiltration"] },
            ["bulk_import"] = new() { BaseRiskScore = 0.75 },
            ["admin_action"] = new() { BaseRiskScore = 0.80, RiskTags = ["privilege_escalation"] },
            ["suspicious"] = new() { BaseRiskScore = 0.85, RiskTags = ["malicious"] },
            ["destructive_delete"] = new() { BaseRiskScore = 0.90, RiskTags = ["destructive"] },
            ["soft_delete"] = new() { BaseRiskScore = 0.90, RiskTags = ["destructive"] },
            ["escalate_privileges"] = new() { BaseRiskScore = 0.95, RiskTags = ["privilege_escalation"] },
            ["harmful"] = new() { BaseRiskScore = 1.00, RiskTags = ["malicious"] }
        };
    }
}
