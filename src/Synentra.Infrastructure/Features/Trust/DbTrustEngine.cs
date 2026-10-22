using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synentra.Application.Common.Persistence;
using Synentra.Application.Features.Trust.Contracts;
using Synentra.Domain.Governance.Trust;
using Synentra.Domain.Governance.Trust.ValueObjects;
using Synentra.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Common;
using Synentra.Persistence.Serialization;
using Synentra.Infrastructure.Configuration.Trust;

namespace Synentra.Infrastructure.Features.Trust;

public sealed class DbTrustEngine(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    IUnitOfWork work,
    PayloadCodec codec, 
    IOptions<TrustConfiguration> options) : ITrustEngine
{
    public string ProfileId => "default-trust";

    public async Task<TrustSnapshot> LoadSnapshotAsync(string agentId, string securityScope, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var id = Keys.Of(agentId, securityScope);

        // Both projections evaluated in a single SQL statement for a consistent snapshot.
        var row = await db.Trusts.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Score, x.Version, x.State, Quarantined = db.Quarantines.Any(q => q.AgentId == agentId && q.SecurityScope == securityScope && q.ReleasedAt == null) })
            .SingleOrDefaultAsync(ct);

        if (row is null)
        {
            var quarantined = await db.Quarantines.AnyAsync(q => q.AgentId == agentId && q.SecurityScope == securityScope && q.ReleasedAt == null, ct);
            return new(new TrustScore(
                options.Value.InitialScore ?? 50), 
                quarantined ? TrustStatus.Quarantined : TrustStatus.Unknown, 
                "0", 
                quarantined, 
                Array.Empty<TrustFactor>(), 
                DateTimeOffset.UtcNow);
        }

        return new(
            new TrustScore(row.Score), 
            row.Quarantined ? TrustStatus.Quarantined : Enum.Parse<TrustStatus>(row.State), 
            row.Version.ToString(), 
            row.Quarantined, new[] { new TrustFactor("evidence", row.Score, "Configured evidence weights.") }, 
            DateTimeOffset.UtcNow);
    }

    public async Task RecordEvidenceAsync(TrustEvidence evidence, CancellationToken ct)
    {
        await work.ExecuteAsync(async token =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(token);
            var evidenceKey = Keys.Of(evidence.AgentId, evidence.SecurityScope, evidence.EvidenceId);
            var old = await db.TrustEvidences.FindAsync(new object[] { evidenceKey }, token);
            if (old is not null)
            {
                if (System.Text.Json.JsonSerializer.Serialize(codec.Decode<TrustEvidence>(old.Payload), PayloadCodec.Json) != System.Text.Json.JsonSerializer.Serialize(evidence, PayloadCodec.Json))
                    throw new InvalidOperationException("Evidence identity conflict.");

                return true;
            }

            db.Add(new TrustEvidenceEntity { Id = evidenceKey, AgentId = evidence.AgentId, SecurityScope = evidence.SecurityScope, EvidenceId = evidence.EvidenceId, Type = evidence.Type.ToString(), ObservedAt = evidence.ObservedAt.ToUnixTimeMilliseconds(), Payload = codec.Encode(evidence) });
            var id = Keys.Of(evidence.AgentId, evidence.SecurityScope);
            var row = await db.Trusts.FindAsync(new object[] { id }, token);
            if (row is null)
            {
                row = new()
                {
                    Id = id,
                    AgentId = evidence.AgentId,
                    SecurityScope = evidence.SecurityScope,
                    Score = options.Value.InitialScore ?? 50,
                    State = "Unknown"
                };
                db.Add(row);
            }

            if (evidence.Type != TrustEvidenceType.InfrastructureFailure)
            {
                if (evidence.Type == TrustEvidenceType.ManualAdjustment)
                    throw new InvalidOperationException("Use authenticated administrative trust adjustment.");

                var delta = options.Value.EvidenceWeights.GetValueOrDefault(evidence.Type.ToString(), 0);
                row.Score = Math.Clamp(row.Score + delta, 0, 100);
                row.State = row.Score < 30 ? "Restricted" : "Normal";
            }

            row.UpdatedAt = Keys.Now;
            if (row.Score <= options.Value.QuarantineThreshold && !await db.Quarantines.AnyAsync(q => q.AgentId == evidence.AgentId && q.SecurityScope == evidence.SecurityScope && q.ReleasedAt == null, token))
                db.Add(new QuarantineEntity { 
                    Id = Guid.NewGuid().ToString("N"), 
                    AgentId = evidence.AgentId, 
                    SecurityScope = evidence.SecurityScope, 
                    Reason = "TRUST_THRESHOLD", 
                    StartedAt = Keys.Now });

            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }
}
