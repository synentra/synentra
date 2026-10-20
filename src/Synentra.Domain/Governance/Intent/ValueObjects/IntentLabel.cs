using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;

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
            throw new DomainException(DomainErrors.Intent.ConfidenceOutOfRange);

        Value = value;
        Confidence = confidence;
        Status = status;
        ProviderVersion = providerVersion;
    }
}
