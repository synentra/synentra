using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Operations;
using Synentra.Extensions;

namespace Synentra.Endpoints.V2;

public sealed class Operations : EndpointGroupBase
{
    public override void Map(WebApplication app) => app.MapGet("/v2/operations", Get).WithTags("Operations");

    private static async Task<IResult> Get(
        int? pageNumber, 
        int? pageSize, 
        HttpContext context, 
        IDispatcher dispatcher, 
        IPrincipalResolver principals, 
        CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var result = await dispatcher.ListOperations(principal.AgentId, pageNumber ?? 1, pageSize ?? 50, ct);
        return result.ToHttpResult();
    }
}
