namespace Synentra.Application.Common.Dispatching;

public interface IDispatcher
{
    Task<TResult> Dispatch<TResult>(IAction<TResult> action, CancellationToken cancellationToken = default);
}
