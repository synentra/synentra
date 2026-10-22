using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Models;
using Synentra.Application.Features.Operations.Contracts;
using Synentra.Domain.Common.Results;

namespace Synentra.Application.Features.Operations.Queries.ListOperations;

public sealed class ListOperationsQueryHandler(
    IManifestProvider manifests, 
    IRevisionProvider revisions) : IActionHandler<ListOperationsQuery, PaginatedResult<OperationDto>>
{
    public async Task<PaginatedResult<OperationDto>> Handle(ListOperationsQuery request, CancellationToken cancellationToken = default)
    {
        await revisions.ReloadAsync(cancellationToken);
        var bundle = manifests.ActiveBundle;
        if (bundle is null) 
            return PaginatedResult<OperationDto>.Empty(request.PageRequest);

        var operations = bundle.Spec.Operations
            .Where(op => !bundle.Spec.Restrictions.Any(r => IsHardDenied(r, request.AgentId, op.Id, op.Family)));

        var totalCount = operations.Count();
        var items = operations
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .Skip((request.PageRequest.PageNumber - 1) * request.PageRequest.PageSize)
            .Take(request.PageRequest.PageSize)
            .Select(x => new OperationDto(
                x.Id, x.Version, 
                x.Family, 
                x.Action, 
                x.Mutability, 
                x.Execution.Model, 
                x.TaskGrantRequired, 
                x.PolicyRefs, 
                bundle.Spec.Bindings.Where(b => b.OperationId == x.Id).Select(b => b.Id).Order().ToArray()))
            .ToArray();

        return await PaginatedResult<OperationDto>.SuccessAsync(items, request.PageRequest, totalCount);
    }

    private static bool IsHardDenied(RestrictionModel restriction, string agentId, string operationId, string family)
    {
        if (!string.Equals(restriction.Effect, "deny", StringComparison.OrdinalIgnoreCase)) 
            return false;

        var m = restriction.Match;
        return Matches(m.AgentIds, agentId) && Matches(m.OperationIds, operationId) && Matches(m.OperationFamilies, family);
    }

    private static bool Matches(IReadOnlyCollection<string>? values, string value)
        => values is null || values.Count == 0 || values.Contains(value, StringComparer.Ordinal);
}
