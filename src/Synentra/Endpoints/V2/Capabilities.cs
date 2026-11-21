using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Capabilities.Queries.GetCapabilities;
using Synentra.Endpoints.V2.Support;

namespace Synentra.Endpoints.V2;

public sealed class Capabilities : EndpointGroupBase
{
    public override void Map(WebApplication app) => app.MapGet("/v2/capabilities", Get).WithTags("Capabilities");

    private static async Task<IResult> Get(
        HttpContext context,
        IDispatcher dispatcher,
        IPrincipalResolver principals,
        CancellationToken ct)
    {
        await context.RequirePrincipalAsync(principals, ct);
        return Results.Ok(await dispatcher.Dispatch(new GetCapabilitiesQuery(), ct));
    }
}
