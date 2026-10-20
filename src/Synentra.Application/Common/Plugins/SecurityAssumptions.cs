namespace Synentra.Application.Common.Plugins;

public sealed record SecurityAssumptions(
    bool TrustedCode, 
    bool RequiresIsolation, 
    string ThreatModel, 
    IReadOnlyList<string> SecretsAccessible);
