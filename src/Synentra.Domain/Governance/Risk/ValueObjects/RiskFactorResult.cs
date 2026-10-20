namespace Synentra.Domain.Governance.Risk.ValueObjects;

public sealed record RiskFactorResult(
    string Name, 
    double Score, 
    double Weight, 
    string Explanation,
    RiskFactorStatus Status);
