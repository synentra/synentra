using Microsoft.EntityFrameworkCore;

namespace Synentra.Persistence;

public sealed class SynentraDbContextFactoryAdapter<TContext>(IDbContextFactory<TContext> inner)
    : IDbContextFactory<SynentraDbContext>
    where TContext : SynentraDbContext
{
    public SynentraDbContext CreateDbContext() => inner.CreateDbContext();

    public async Task<SynentraDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => await inner.CreateDbContextAsync(cancellationToken);
}