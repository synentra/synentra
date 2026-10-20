using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Capabilities.Queries.GetCapabilities;

namespace Synentra.Endpoints.V1;

public sealed class Capabilities : EndpointGroupBase
{
    public override void Map(WebApplication app) => app.MapGet("/v1/capabilities", Get).WithTags("Capabilities");
    private static async Task<IResult> Get(HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        return Results.Ok(await dispatcher.Dispatch(new GetCapabilitiesQuery(), ct));
    }
}
