using System.Text;
using Microsoft.AspNetCore.Mvc;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Endpoints.V2.Support;
using Synentra.Application.Features.Manifests;
using Synentra.Diagnostics.Catalog;

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
        CancellationToken cancellationToken)
    {
        await context.RequireAdminAsync(principals, cancellationToken);
        if (!context.Request.ContentType.IsSupportedManifestMediaType()) 
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        
        using var ms = new MemoryStream();
        await context.Request.Body.CopyToAsync(ms, cancellationToken); ms.Position = 0;
        var result = await dispatcher.ValidateManifest(ms, cancellationToken);

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
        CancellationToken cancellationToken)
    {
        await context.RequireAdminAsync(principals, cancellationToken);
        if (!context.Request.ContentType.IsSupportedManifestMediaType()) 
            return context.UnsupportedMediaType("UNSUPPORTED_MEDIA_TYPE", "The request media type is not supported.");

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);
        using var parseStream = new MemoryStream(Encoding.UTF8.GetBytes(body));
        var bundle = parser.Parse(parseStream);
        if (!string.Equals(bundle.Metadata.Name, id, StringComparison.Ordinal)) 
            return context.Conflict(ManifestDiagnostics.ManifestIdMismatch);

        var revision = await dispatcher.StoreManifest(body, cancellationToken);
        return Results.Ok(new { id, version = bundle.Metadata.Version, revision });
    }

    private static async Task<IResult> Activate(
        string id, 
        ActivateManifestRequest request, 
        HttpContext context, 
        IDispatcher dispatcher, 
        IPrincipalResolver principals, 
        CancellationToken cancellationToken)
    {
        await context.RequireAdminAsync(principals, cancellationToken);
        var result = await dispatcher.Activate(id, request.Version, request.ExpectedActiveRevision, cancellationToken);
        return result.Activated 
            ? Results.Ok(result) 
            : Results.BadRequest(result);
    }

    public sealed record ActivateManifestRequest(string Version, string? ExpectedActiveRevision);
}
