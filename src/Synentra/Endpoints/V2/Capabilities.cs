using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Capabilities;
using Synentra.Endpoints.V2.Support;

namespace Synentra.Endpoints.V2;

public sealed class Capabilities : EndpointGroupBase
{
    public override void Map(WebApplication app) => app.MapGet("/v2/capabilities", Get).WithTags("Capabilities");

    private static async Task<IResult> Get(
        HttpContext context,
        IDispatcher dispatcher,
        IPrincipalResolver principals,
        CancellationToken cancellationToken)
    {
        await context.RequirePrincipalAsync(principals, cancellationToken);
        var result = await dispatcher.GetCapabilities(cancellationToken);
        return Results.Ok(result);
    }
}
