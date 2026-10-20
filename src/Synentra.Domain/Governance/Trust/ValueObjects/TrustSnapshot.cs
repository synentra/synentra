namespace Synentra.Domain.Governance.Trust.ValueObjects;

public sealed record TrustSnapshot(
    TrustScore Score, 
    TrustStatus Status, 
    string Version,
    bool Quarantined, 
    IReadOnlyList<TrustFactor> Factors, 
    DateTimeOffset SnapshotAt)
{
    public static TrustSnapshot Unknown 
        => new(new TrustScore(0), TrustStatus.Unknown, "0", false, Array.Empty<TrustFactor>(), DateTimeOffset.UtcNow);
}
