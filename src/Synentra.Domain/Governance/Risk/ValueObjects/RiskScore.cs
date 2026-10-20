using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Domain.Governance.Risk.ValueObjects;

public sealed record RiskScore
{
    public double Value { get; }
    public double Coverage { get; } // 0.0 - 1.0

    public RiskScore(double value, double coverage)
    {
        if (value < 0 || value > 1)
            throw new DomainException(DomainErrors.Risk.RiskScoreOutOfRange);

        if (coverage < 0 || coverage > 1)
            throw new DomainException(DomainErrors.Risk.CoverageOutOfRange);

        Value = value;
        Coverage = coverage;
    }
}
