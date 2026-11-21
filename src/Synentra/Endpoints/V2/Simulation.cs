using Microsoft.AspNetCore.Mvc;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Invocations;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Endpoints.V2.Support;

namespace Synentra.Endpoints.V2;

public sealed class Simulation : EndpointGroupBase
{
    public override void Map(WebApplication app) 
        => app.MapPost("/v2/simulate", Simulate)
              .WithTags("Simulation")
              .WithMetadata(new RequestSizeLimitAttribute(256 * 1024));

    private static async Task<IResult> Simulate(
        InvocationRequest request,
        HttpContext context,
        IDispatcher dispatcher,
        IPrincipalResolver principals,
        CancellationToken cancellationToken)
    {
        var principal = await context.RequirePrincipalAsync(principals, cancellationToken);
        var result = await dispatcher.Simulate(request, principal, cancellationToken);
        return Results.Ok(new { authorizing = false, result });
    }
}
