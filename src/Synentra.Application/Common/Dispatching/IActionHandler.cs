namespace Synentra.Application.Common.Dispatching;

public interface IActionHandler<TAction, TResult>
    where TAction : IAction<TResult>
{
    Task<TResult> Handle(TAction action, CancellationToken cancellationToken = default);
}
