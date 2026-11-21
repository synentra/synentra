using System.Text;
using Microsoft.AspNetCore.Mvc;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Manifests.Commands.ActivateManifest;
using Synentra.Application.Features.Manifests.Commands.StoreManifest;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Queries.ValidateManifest;
using Synentra.Endpoints.V2.Support;

namespace Synentra.Endpoints.V2;

public sealed class Manifests : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup("/v2/manifests").WithTags("Manifests");
        group.MapPost("/validate", Validate)
            .Accepts<string>("application/json", "application/yaml", "text/yaml")
            .WithMetadata(new RequestSizeLimitAttribute(512 * 1024));

        group.MapPut("/{id}", Store)
            .Accepts<string>("application/json", "application/yaml", "text/yaml")
            .WithMetadata(new RequestSizeLimitAttribute(512 * 1024));

        group.MapPost("/{id}/activate", Activate);
    }

    private static async Task<IResult> Validate(
        HttpContext context, 
        IDispatcher dispatcher, 
        IPrincipalResolver principals, 
        CancellationToken ct)
    {
        await context.RequireAdminAsync(principals, ct);
        if (!context.Request.ContentType.IsSupportedManifestMediaType()) 
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        
        using var ms = new MemoryStream();
        await context.Request.Body.CopyToAsync(ms, ct); ms.Position = 0;
        var result = await dispatcher.Dispatch(new ValidateManifestQuery(ms), ct);

        if (result.IsValid)
            return Results.Ok(result);

        return context.ValidationFailed(result.Issues, "MANIFEST_VALIDATION_FAILED", "Manifest validation failed.");
    }

    private static async Task<IResult> Store(
        string id, 
        HttpContext context, 
        IDispatcher dispatcher, 
        IPrincipalResolver principals, 
        IBundleParser parser, 
        CancellationToken ct)
    {
        await context.RequireAdminAsync(principals, ct);
        if (!context.Request.ContentType.IsSupportedManifestMediaType()) 
            return context.UnsupportedMediaType("UNSUPPORTED_MEDIA_TYPE", "The request media type is not supported.");

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(ct);
        using var parseStream = new MemoryStream(Encoding.UTF8.GetBytes(body));
        var bundle = parser.Parse(parseStream);
        if (!string.Equals(bundle.Metadata.Name, id, StringComparison.Ordinal)) 
            return context.Conflict("MANIFEST_ID_MISMATCH", "The manifest identifier does not match the request.");

        var revision = await dispatcher.Dispatch(new StoreManifestCommand(body), ct);
        return Results.Ok(new { id, version = bundle.Metadata.Version, revision });
    }

    private static async Task<IResult> Activate(
        string id, 
        ActivateManifestRequest request, 
        HttpContext context, 
        IDispatcher dispatcher, 
        IPrincipalResolver principals, 
        CancellationToken ct)
    {
        await context.RequireAdminAsync(principals, ct);
        var result = await dispatcher.Dispatch(new ActivateManifestCommand(id, request.Version, request.ExpectedActiveRevision), ct);
        return result.Activated 
            ? Results.Ok(result) 
            : Results.BadRequest(result);
    }

    public sealed record ActivateManifestRequest(string Version, string? ExpectedActiveRevision);
}
