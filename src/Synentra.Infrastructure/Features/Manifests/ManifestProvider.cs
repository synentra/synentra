using Microsoft.EntityFrameworkCore;
using Synentra.Persistence;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Models;
using System.Text;

namespace Synentra.Infrastructure.Features.Manifests;

public sealed class ManifestProvider(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    IBundleParser parser) : IManifestProvider, IManifestProviderInternal, IRevisionProvider
{
    private sealed record Snapshot(GovernanceBundleModel? Bundle, string Revision);
    private Snapshot snapshot = new(null, "");
    public GovernanceBundleModel? ActiveBundle => snapshot.Bundle;
    public string ActiveRevision => snapshot.Revision;

    public async Task ReloadAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await (
            from active in db.ActiveManifests.AsNoTracking() 
            join manifest in db.Manifests.AsNoTracking() on active.ManifestVersionId equals manifest.Id
            where active.Id == 1
            select manifest).SingleOrDefaultAsync(ct);

        if (row is null)
        {
            snapshot = new(null, "");
            return;
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(row.Bundle));
        snapshot = new(parser.Parse(stream), row.Revision);
    }

    public void SetActive(GovernanceBundleModel bundle, string revision) => snapshot = new(bundle, revision);
}
