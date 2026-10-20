namespace Synentra.Application.Common.Dispatching;

public interface ICommand<TResult> : IOperation<TResult>
{
}

public interface ICommand : ICommand<Void>
{
}
