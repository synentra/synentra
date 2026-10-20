using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Persistence;
using Synentra.Persistence.Entities;
using Synentra.Persistence.Common;
using System.Security.Cryptography;
using System.Text;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Exceptions;

namespace Synentra.Persistence.Features.Manifests;

public sealed class ManifestStore(
    IDbContextFactory<SynentraDbContext> dbFactory, 
    IUnitOfWork work) : IManifestStore
{
    public Task<string> StoreVersionAsync(string manifestId, string version, string bundleJson, string contentHash, CancellationToken ct) => work.ExecuteAsync(async token =>
    {
        await using var db = await dbFactory.CreateDbContextAsync(token);
        var actual = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(bundleJson)));

        if (!string.Equals(actual, contentHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Content digest mismatch.");

        var id = Keys.Of(manifestId, version);
        var row = await db.Manifests.FindAsync(new object[] { id }, token);
        if (row is not null)
        {
            if (row.ContentHash != actual)
                throw new InvalidOperationException("MANIFEST_VERSION_CONFLICT");

            return row.Revision;
        }

        // Global revision token distinguishes manifests with identical semantic versions.
        var revision = Keys.Of(manifestId, version, actual);
        db.Add(new ManifestEntity { Id = id, ManifestId = manifestId, Revision = revision, ContentHash = actual, Bundle = bundleJson, StoredAt = Keys.Now });
        await db.SaveChangesAsync(token);
        return revision;
    }, ct: ct);

    public async Task<ManifestVersionSnapshot?> GetVersionAsync(string manifestId, string revision, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.Manifests.AsNoTracking().SingleOrDefaultAsync(x => x.ManifestId == manifestId && x.Revision == revision, ct);
        return row is null ? null : new(row.ManifestId, row.Revision, row.Bundle);
    }

    public async Task<ManifestVersionSnapshot?> GetVersionByVersionAsync(string manifestId, string version, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var id = Keys.Of(manifestId, version);
        var row = await db.Manifests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ManifestId == manifestId, ct);
        return row is null ? null : new(row.ManifestId, row.Revision, row.Bundle);
    }
    public async Task<string?> GetActiveRevisionAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return (await db.ActiveManifests.AsNoTracking().SingleAsync(x => x.Id == 1, ct)).Revision;
    }
    public Task<string> ActivateAtomicallyAsync(string manifestId, string revision, string? expectedCurrent, CancellationToken ct) => work.ExecuteAsync(async token =>
    {
        await using var db = await dbFactory.CreateDbContextAsync(token);
        var current = await db.ActiveManifests.SingleAsync(x => x.Id == 1, token);

        if (current.Revision != expectedCurrent)
            throw new ActivationConflictException(current.Revision ?? "", expectedCurrent);

        var target = await db.Manifests.SingleAsync(x => x.ManifestId == manifestId && x.Revision == revision, token);
        current.ManifestVersionId = target.Id;
        current.Revision = target.Revision;
        await db.SaveChangesAsync(token);
        return current.Revision;
    }, ct: ct);
}
