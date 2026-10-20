using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synentra.Application.Common.Persistence;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Domain.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Configuration;
using Synentra.Persistence;
using Synentra.Persistence.Common;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Serialization;

namespace Synentra.Infrastructure.Features.Trust;

public sealed class TrustAdministrationService(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    IUnitOfWork work, 
    IAdminPolicy policy, 
    IOptions<InfrastructureOptions> options, 
    IAuditStore audit, 
    PayloadCodec codec)
{
    public Task<double> AdjustAsync(string requestId, string agentId, string scope, double delta, string reason, CanonicalPrincipal actor, CancellationToken ct) => work.ExecuteAsync(async token =>
    {
        await using var db = await dbFactory.CreateDbContextAsync(token);
        if (actor.Type != PrincipalType.Admin || actor.SecurityScope != scope)
            throw new UnauthorizedAccessException();

        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (!double.IsFinite(delta) || Math.Abs(delta) > policy.MaxManualTrustAdjustment)
            throw new ArgumentOutOfRangeException(nameof(delta));

        var eventId = Keys.Of("manual-trust", scope, requestId);
        var old = await db.Audits.FindAsync(new object[] { eventId }, token);
        if (old is not null)
        {
            var prior = codec.Decode<AuditEntry>(old.Payload);
            if (prior.ActorId != actor.AgentId 
            || prior.Details.GetValueOrDefault("agentId") != agentId 
            || prior.Details.GetValueOrDefault("delta") != delta.ToString("R", System.Globalization.CultureInfo.InvariantCulture) 
            || prior.Details.GetValueOrDefault("reason") != reason)
                throw new InvalidOperationException("Adjustment request conflict.");

            return double.Parse(prior.Details["score"], System.Globalization.CultureInfo.InvariantCulture);
        }

        var id = Keys.Of(agentId, scope);
        var row = await db.Set<TrustEntity>().FindAsync(new object[] { id }, token);
        if (row is null)
        {
            row = new()
            {
                Id = id,
                AgentId = agentId,
                SecurityScope = scope,
                Score = options.Value.InitialTrustScore,
                State = "Unknown"
            };
            db.Add(row);
        }

        row.Score = Math.Clamp(row.Score + delta, 0, 100);
        row.State = row.Score < 30 ? "Restricted" : "Normal";
        row.UpdatedAt = Keys.Now;

        if (row.Score <= options.Value.QuarantineThreshold && !await db.Quarantines.AnyAsync(q => q.AgentId == agentId && q.SecurityScope == scope && q.ReleasedAt == null, token))
            db.Add(new QuarantineEntity { Id = Guid.NewGuid().ToString("N"), AgentId = agentId, SecurityScope = scope, Reason = "TRUST_THRESHOLD", StartedAt = Keys.Now });

        // Raising trust never silently releases a quarantine. Release is a separate authorized operation.
        await audit.AppendAsync(new AuditEntry(
            eventId, "manual-trust-adjustment", 
            actor.AgentId, 
            scope, 
            null, 
            null, 
            null, 
            null, 
            null, 
            null, 
            null, 
            new Dictionary<string, 
            string> { 
                ["agentId"] = agentId, 
                ["delta"] = delta.ToString("R", 
                System.Globalization.CultureInfo.InvariantCulture), 
                ["score"] = row.Score.ToString("R", System.Globalization.CultureInfo.InvariantCulture), 
                ["reason"] = reason }, 
            DateTimeOffset.UtcNow), 
            token);

        return row.Score;
    }, ct: ct);
}
