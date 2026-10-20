using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Domain.Security;
using Synentra.Application.Features.Auditing;
using Synentra.Extensions;

namespace Synentra.Endpoints.V1;

public sealed class Audits : EndpointGroupBase
{
    public override void Map(WebApplication app) => app.MapGet("/v1/audits", Query).WithTags("Audit");

    private static async Task<IResult> Query(
        string? actorId, 
        string? invocationId, 
        string? category, 
        DateTimeOffset? from, 
        DateTimeOffset? to, 
        int? pageNumber, 
        int? pageSize, 
        HttpContext context, 
        IDispatcher dispatcher, 
        IPrincipalResolver principals, 
        CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var effectiveActorId = principal.Type == PrincipalType.Admin ? actorId : principal.AgentId;

        var filter = new AuditQuery(principal.SecurityScope, effectiveActorId, invocationId, category, from, to);

        var results = await dispatcher.QueryAudits(filter, pageNumber, pageSize, ct);
        return results.ToHttpResult();
    }
}
