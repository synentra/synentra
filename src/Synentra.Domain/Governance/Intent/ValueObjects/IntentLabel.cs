using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;

namespace Synentra.Domain.Governance.Intent.ValueObjects;

public sealed record IntentLabel
{
    public string Value { get; }
    public double Confidence { get; }
    public IntentStatus Status { get; }
    public string ProviderVersion { get; }

    public IntentLabel(
        string value, 
        double confidence, 
        IntentStatus status, 
        string providerVersion)
    {
        if (confidence < 0 || confidence > 1)
            throw new SynentraDomainException(IntentDiagnostics.ConfidenceOutOfRange);

        Value = value;
        Confidence = confidence;
        Status = status;
        ProviderVersion = providerVersion;
    }
}
