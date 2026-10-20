using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Domain.Governance.Trust.ValueObjects;

public sealed record TrustScore
{
    public double Value { get; } // existing range (e.g., 0-100)

    public TrustScore(double value)
    {
        if (value < 0 || value > 100)
            throw new DomainException(DomainErrors.Trust.TrustScoreOutOfRange);

        Value = value;
    }
}
