namespace Synentra.Application.Common.Messaging;

public interface IOutbox
{
    Task EnqueueAsync<T>(T message, CancellationToken ct) where T : class;
}
