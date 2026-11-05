using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Persistence;
using Synentra.Persistence.Common;

namespace Synentra.Infrastructure.Features.Executions;

public sealed class ClaimRecoveryService(
    IDbContextScope scope,
    IUnitOfWork work,
    IExecutionRecordStore records)
{
    public async Task SweepAsync(CancellationToken ct)
    {
        await work.ExecuteAsync(async token =>
        {
            var db = scope.Current;
            var cutoff = Keys.Now - (long)TimeSpan.FromMinutes(5).TotalMilliseconds;
            var rows = await db.ExecutionClaims
                    .Where(x => (x.State == "Dispatching" || x.State == "Claimed") && x.ExpiresAt < cutoff)
                    .ToListAsync(token);

            foreach (var row in rows)
            {
                var uncertain = row.State == "Dispatching";
                row.State = uncertain ? "Unknown" : "Expired";
                var execution = await records.FindByInvocationAsync(row.InvocationId, token);
                if (execution is not null)
                {
                    if (uncertain)
                        execution.MarkUnknown("WORKER_INTERRUPTED");
                    else
                        execution.MarkFailedBeforeDispatch("CLAIM_EXPIRED");
                    await records.SaveAsync(execution, token);
                }
            }

            return true;
        }, ct: ct);
    }
}
