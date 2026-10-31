using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Features.Secrets;
using Synentra.Persistence;
using Synentra.Persistence.Features.Security;

namespace Synentra.Endpoints.V1;

public sealed class Secrets : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/v1/admin/credentials"))
            {
                if (context.Request.ContentLength > 70000) { context.Response.StatusCode = 413; return; }
                var limit = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                if (limit is { IsReadOnly: false }) limit.MaxRequestBodySize = 70000;
            }
            await next(context);
        });

        var group = app.MapGroup("/v1/admin/credentials").WithTags("Secret administration");

        group.MapPut("/secrets/{path}/{version}", async (string path, string version, StoreSecret request, HttpContext context,
            IPrincipalResolver principals, EncryptedDbCredentialProvider secrets, CancellationToken ct) =>
        {
            var admin = await ApiEndpointSupport.RequireAdminAsync(context, principals, ct);
            await secrets.StoreVersionAsync(path, version, request.Scheme, request.Value, request.ExpiresAt, ct, admin.AgentId);
            return Results.NoContent();
        }).WithMetadata(new RequestSizeLimitAttribute(70000));

        group.MapDelete("/secrets/{path}/{version}", async (string path, string version, HttpContext context,
            IPrincipalResolver principals, EncryptedDbCredentialProvider secrets, CancellationToken ct) =>
        {
            var admin = await ApiEndpointSupport.RequireAdminAsync(context, principals, ct);
            await secrets.RevokeVersionAsync(path, version, ct, admin.AgentId);
            return Results.NoContent();
        });

        group.MapPut("/bindings/{**id}", async (string id, PutBinding request, HttpContext context,
            IPrincipalResolver principals, CredentialBindingStore bindings, CancellationToken ct) =>
        {
            var admin = await ApiEndpointSupport.RequireAdminAsync(context, principals, ct);
            if (request.Binding is null || id != request.Binding.Id) return Results.BadRequest();
            await bindings.SaveAsync(request.Binding.ToDomain(), request.ExpectedVersion, admin.AgentId, materialOnly: false, ct);
            return Results.NoContent();
        });

        group.MapGet("/bindings/{**id}", async (string id, HttpContext context, IPrincipalResolver principals, CredentialBindingStore bindings, CancellationToken ct) =>
        {
            await ApiEndpointSupport.RequireAdminAsync(context, principals, ct);
            var binding = await bindings.GetAsync(id, ct);
            return binding is null ? Results.NotFound() : Results.Ok(binding);
        });

        group.MapPost("/rotate", async (RotateBinding request, HttpContext context, IPrincipalResolver principals, CredentialBindingStore bindings, CancellationToken ct) =>
        {
            var admin = await ApiEndpointSupport.RequireAdminAsync(context, principals, ct);
            if (request.SecretRef is null || string.IsNullOrWhiteSpace(request.BindingId)) return Results.BadRequest();
            var prior = await bindings.GetAsync(request.BindingId, ct);
            if (prior is null) return Results.NotFound();
            var rotated = new CredentialBinding(prior.Id, prior.TargetRef, prior.AllowedPrincipal, prior.DelegationScope, prior.AllowedOperations,
                request.SecretRef, Guid.NewGuid().ToString("N"), false, prior.AuthorityVersion);
            await bindings.SaveAsync(rotated, request.ExpectedVersion, admin.AgentId, materialOnly: true, ct);
            return Results.Ok(new { version = rotated.Version, authorityVersion = rotated.AuthorityVersion });
        });

        group.MapGet("/state", async (string providerId, string path, string version, HttpContext context, IPrincipalResolver principals, SecretStateStore states, CancellationToken ct) =>
        {
            await ApiEndpointSupport.RequireAdminAsync(context, principals, ct);
            if (!SecretPlan.RemoteProviders.Contains(providerId)) return Results.BadRequest();
            return Results.Ok(await states.GetAsync(new(providerId, path, version), ct));
        });

        group.MapPost("/state", async (ChangeSecretState request, HttpContext context, IPrincipalResolver principals, SecretStateStore states, CancellationToken ct) =>
        {
            var admin = await ApiEndpointSupport.RequireAdminAsync(context, principals, ct);
            if (request.SecretRef is null || !SecretPlan.RemoteProviders.Contains(request.SecretRef.ProviderId) || string.IsNullOrWhiteSpace(request.SecretRef.SecretPath) || string.IsNullOrWhiteSpace(request.SecretRef.Version)) return Results.BadRequest();
            var generation = await states.ChangeAsync(request.SecretRef, request.ExpectedGeneration, request.Revoke, admin.AgentId, ct);
            return Results.Ok(new { generation, revoked = request.Revoke });
        });

        group.MapGet("/secrets", async (int? page, HttpContext context, IPrincipalResolver principals, IDbContextFactory<SynentraDbContext> dbFactory, CancellationToken ct) =>
        {
            await ApiEndpointSupport.RequireAdminAsync(context, principals, ct);
            var number = Math.Clamp(page ?? 1, 1, 10000);
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var rows = await EntityFrameworkQueryableExtensions.ToArrayAsync(
                db.Secrets.OrderBy(x => x.Id).Skip((number - 1) * 50).Take(50).Select(x => new { x.Path, x.SecretVersion, x.Type, x.ExpiresAt }), ct);
            return Results.Ok(new { page = number, items = rows });
        });

        group.MapDelete("/bindings/{**id}", async (string id, string expectedVersion, HttpContext context,
            IPrincipalResolver principals, CredentialBindingStore bindings, CancellationToken ct) =>
        {
            var admin = await ApiEndpointSupport.RequireAdminAsync(context, principals, ct);
            var previous = await bindings.GetAsync(id, ct);
            if (previous is null) return Results.NotFound();
            var revoked = new CredentialBinding(previous.Id, previous.TargetRef, previous.AllowedPrincipal, previous.DelegationScope,
                previous.AllowedOperations, previous.SecretRef, Guid.NewGuid().ToString("N"), true);
            await bindings.SaveAsync(revoked, expectedVersion, admin.AgentId, materialOnly: false, ct);
            return Results.NoContent();
        });
    }
    public sealed record RotateBinding(string BindingId, SecretProviderRef SecretRef, string ExpectedVersion);

    public sealed record ChangeSecretState(SecretProviderRef SecretRef, long ExpectedGeneration, bool Revoke);

    public sealed record StoreSecret(string Scheme, string Value, DateTimeOffset? ExpiresAt);

    public sealed record BindingRequest(string Id, string TargetRef, string AllowedPrincipal, string DelegationScope,
        string[] AllowedOperations, SecretProviderRef SecretRef, string Version)
    {
        public CredentialBinding ToDomain() => new(Id, TargetRef, AllowedPrincipal, DelegationScope,
            new HashSet<string>(AllowedOperations ?? throw new ArgumentException("Operations required."), StringComparer.Ordinal), SecretRef ?? throw new ArgumentException("Secret reference required."), Version);
    }

    public sealed record PutBinding(BindingRequest Binding, string? ExpectedVersion);
}