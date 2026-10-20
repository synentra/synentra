namespace Synentra.Application.Common.Messaging;

public interface IOutboxProcessor
{
    Task ProcessPendingAsync(CancellationToken ct);
}
