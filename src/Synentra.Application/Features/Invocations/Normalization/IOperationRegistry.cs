namespace Synentra.Application.Features.Invocations.Normalization;

public interface IOperationRegistry
{
    OperationRegistration? Find(string operationId, string bindingId);
}
