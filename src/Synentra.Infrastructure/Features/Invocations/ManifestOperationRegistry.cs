using Synentra.Application.Features.Invocations.Normalization;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Domain.Operations;

namespace Synentra.Infrastructure.Features.Invocations;

public sealed class ManifestOperationRegistry(IManifestProvider manifests) : IOperationRegistry
{
    public OperationRegistration? Find(string operationId, string bindingId)
    {
        var bundle = manifests.ActiveBundle;
        if (bundle?.Spec.Bindings.Any(b => b.Id == bindingId && b.OperationId == operationId) != true)
            return null;

        var op = bundle.Spec.Operations.SingleOrDefault(x => x.Id == operationId);
        return op is null 
            ? null 
            : new(op.Id, op.Version, op.Family, op.Action, op.Mutability == "read_only" ? Mutability.ReadOnly : Mutability.Mutation, ExecutionModel.RequestResponse, op.TaskGrantRequired, op.Effect is not null, op.PolicyRefs);
    }
}
