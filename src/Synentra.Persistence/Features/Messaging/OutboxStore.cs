using Synentra.Application.Common.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Serialization;
using Synentra.Persistence.Common;
using Synentra.Application.Common.Messaging;
using Synentra.Application.Features.Executions.Messages;
using Microsoft.EntityFrameworkCore;

namespace Synentra.Persistence.Features.Messaging;

public sealed class OutboxStore(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    PayloadCodec codec, 
    IUnitOfWork work) : IOutbox
{
    public async Task EnqueueAsync<T>(T message, CancellationToken ct)
        where T : class
    {
        if (message is not DispatchCommand)
            throw new InvalidOperationException("Unregistered outbox message type.");

        await work.ExecuteAsync(async token =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(token);
            db.Add(new OutboxEntity { Id = Guid.NewGuid().ToString("N"), Type = "dispatch.v1", Payload = codec.Encode(message), CreatedAt = Keys.Now });
            await db.SaveChangesAsync(token);
            return true;
        }, ct: ct);
    }
}
