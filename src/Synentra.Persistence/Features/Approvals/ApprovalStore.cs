using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Domain.Approvals;
using Synentra.Domain.Common.Results;
using Synentra.Persistence.Common;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Serialization;
using System.Text.Json;

namespace Synentra.Persistence.Features.Approvals;

public sealed class ApprovalStore(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    IDbContextScope scope, 
    PayloadCodec codec, 
    IUnitOfWork work) : IApprovalStore
{
    private Approval Map(ApprovalEntity r) => AggregateReader.Approval(codec.Decode<JsonElement>(r.Payload));

    public async Task<Approval?> GetAsync(string id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var r = await db.Set<ApprovalEntity>().FindAsync(new object[] { id }, ct);
        return r is null ? null : Map(r);
    }

    public async Task SaveAsync(Approval value, CancellationToken ct)
    {
        await work.ExecuteAsync(async token =>
        {
            var db = scope.Current;
            var r = await db.Approvals.FindAsync(new object[] { value.Id.ToString() }, token);
            if (r is null)
            {
                r = new ApprovalEntity
                {
                    Id = value.Id.ToString()
                };
                db.Add(r);
            }

            r.InvocationId = value.InvocationId.ToString();
            r.SecurityScope = value.SecurityScope;
            r.State = value.Status.ToString();
            r.ExpiresAt = value.ExpiresAt.ToUnixTimeMilliseconds();
            r.CreatedAt = value.CreatedAt.ToUnixTimeMilliseconds();
            r.Payload = codec.Encode(value);
            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }

    public async Task<Approval?> FindByInvocationAsync(string invocationId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var r = await db.Approvals.SingleOrDefaultAsync(x => x.InvocationId == invocationId, ct);
        return r is null ? null : Map(r);
    }

    public async Task<Page<Approval>> ListPendingAsync(string securityScope, PageRequest pageRequest, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = Keys.Now;

        var totalCount = await db.Approvals.CountAsync(ct);

        var entities = await db.Approvals
            .Where(x => x.SecurityScope == securityScope && x.State == "Pending" && x.ExpiresAt > now)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((pageRequest.PageNumber - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync(ct);

        var items = entities
            .Select(Map)
            .ToList();

        return new Page<Approval>(items, totalCount);
    }
}
