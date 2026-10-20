namespace Synentra.Application.Common.Persistence;

public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation, 
        Func<T, bool>? commitWhen = null, 
        CancellationToken ct = default);
}
