namespace Synentra.Domain.Governance.Trust.ValueObjects;

public sealed record TrustFactor(
    string Name, 
    double Value, 
    string Explanation);
