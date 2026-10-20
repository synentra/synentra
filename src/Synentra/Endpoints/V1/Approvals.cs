using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Approvals;
using Synentra.Application.Features.Approvals.Commands.ApproveInvocation;
using Synentra.Application.Features.Approvals.Commands.RejectInvocation;
using Synentra.Application.Features.Approvals.Commands.RevokeApproval;
using Synentra.Application.Features.Approvals.Queries.GetApproval;
using Synentra.Domain.Security;
using Synentra.Extensions;

namespace Synentra.Endpoints.V1;

public sealed class Approvals : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var g = app.MapGroup("/v1/approvals").WithTags("Approvals");
        g.MapGet("", List);
        g.MapGet("/{id}", Get);
        g.MapPost("/{id}/approve", Approve);
        g.MapPost("/{id}/reject", Reject);
        g.MapPost("/{id}/revoke", Revoke);
    }

    private static async Task<IResult> List(int? pageNumber, int? pageSize, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);

        if (principal.Type is not (PrincipalType.Admin or PrincipalType.Reviewer)) 
            throw new ForbiddenApiException("Reviewer permission required.");

        var result = await dispatcher.ListPendingApprovals(principal.SecurityScope, pageNumber, pageSize, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Get(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        var approval = await dispatcher.Dispatch(new GetApprovalQuery(id), ct);
        if (approval is null) return Results.NotFound();
        if (principal.Type != PrincipalType.Admin && principal.SecurityScope != approval.SecurityScope && principal.AgentId != approval.RequestingAgentId) return Results.NotFound();
        return Results.Ok(approval);
    }

    private static async Task<IResult> Approve(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, IAdminPolicy policy, CancellationToken ct)
    {
        var reviewer = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        await dispatcher.Dispatch(new ApproveInvocationCommand(id, reviewer, policy.AllowsSelfApproval), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Reject(string id, RejectApprovalRequest? request, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var reviewer = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        await dispatcher.Dispatch(new RejectInvocationCommand(id, reviewer, request?.Reason), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Revoke(string id, HttpContext context, IDispatcher dispatcher, IPrincipalResolver principals, CancellationToken ct)
    {
        var principal = await ApiEndpointSupport.RequirePrincipalAsync(context, principals, ct);
        await dispatcher.Dispatch(new RevokeApprovalCommand(id, principal), ct);
        return Results.NoContent();
    }

    public sealed record RejectApprovalRequest(string? Reason);
}
