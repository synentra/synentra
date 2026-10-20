namespace Synentra.Application.Common.Dispatching;

public interface IOperation<TResult> : IAction<TResult>
{
}

public interface IOperation : IOperation<Void>
{
}
