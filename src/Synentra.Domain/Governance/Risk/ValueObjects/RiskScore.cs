using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;

namespace Synentra.Domain.Governance.Risk.ValueObjects;

public sealed record RiskScore
{
    public double Value { get; }
    public double Coverage { get; } // 0.0 - 1.0

    public RiskScore(double value, double coverage)
    {
        if (value < 0 || value > 1)
            throw new SynentraDomainException(RiskDiagnostics.RiskScoreOutOfRange);

        if (coverage < 0 || coverage > 1)
            throw new SynentraDomainException(RiskDiagnostics.CoverageOutOfRange);

        Value = value;
        Coverage = coverage;
    }
}
