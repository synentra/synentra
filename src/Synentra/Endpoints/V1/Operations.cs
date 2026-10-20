using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Operations.Queries.ListOperations;

namespace Synentra.Endpoints.V1;

public sealed class Operations : EndpointGroupBase
{
    public override void Map(WebApplication app) => app.MapGet("/v1/operations", Get).WithTags("Operations");
    private static async Task<IResult> Get(int? pageNumber, int? pageSize, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var page = pageNumber is > 0 ? pageNumber.Value : 1; var size = Math.Clamp(pageSize is > 0 ? pageSize.Value : 50, 1, 200);
        var items = await dispatcher.Dispatch(new ListOperationsQuery(principal.AgentId, page, size), ct);
        return Results.Ok(new { items, pageNumber = page, pageSize = size });
    }
}
