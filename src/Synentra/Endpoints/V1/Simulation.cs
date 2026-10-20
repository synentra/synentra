using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Invocations.Commands.SimulateInvocation;
using Synentra.Application.Features.Invocations.Contracts;

namespace Synentra.Endpoints.V1;

public sealed class Simulation : EndpointGroupBase
{
    public override void Map(WebApplication app) => app.MapPost("/v1/simulate", Simulate).WithTags("Simulation").WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(256 * 1024));
    private static async Task<IResult> Simulate(InvocationRequest request, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var result = await dispatcher.Dispatch(new SimulateInvocationCommand(request, principal), ct);
        return Results.Ok(new { authorizing = false, result });
    }
}
