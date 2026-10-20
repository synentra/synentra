using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Serialization;
using System.Text.Json;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Domain.Execution;

namespace Synentra.Persistence.Features.Executions;

public sealed class ExecutionRecordStore(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    IDbContextScope scope, 
    PayloadCodec codec, 
    IUnitOfWork work) : IExecutionRecordStore
{
    private ExecutionRecord Map(ExecutionEntity r) => AggregateReader.Execution(codec.Decode<JsonElement>(r.Payload));

    public async Task<ExecutionRecord?> GetAsync(string id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var r = await db.Executions.FindAsync(new object[] { id }, ct);
        return r is null ? null : Map(r);
    }

    public async Task SaveAsync(ExecutionRecord value, CancellationToken ct)
    {
        await work.ExecuteAsync(async token =>
        {
            var db = scope.Current;
            var r = await db.Executions.FindAsync(new object[] { value.Id.ToString() }, token);
            if (r is null)
            {
                r = new ExecutionEntity
                {
                    Id = value.Id.ToString()
                };
                db.Add(r);
            }

            r.InvocationId = value.InvocationId.ToString();
            r.State = value.Status.ToString();
            r.Payload = codec.Encode(value);
            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }

    public async Task<ExecutionRecord?> FindByInvocationAsync(string invocationId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var r = await db.Executions.SingleOrDefaultAsync(x => x.InvocationId == invocationId, ct);
        return r is null ? null : Map(r);
    }
}
