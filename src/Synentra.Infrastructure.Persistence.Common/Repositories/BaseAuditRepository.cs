using Microsoft.EntityFrameworkCore;
using Synentra.Application.Abstractions.Persistence;
using Synentra.Domain.AuditTrails;

namespace Synentra.Infrastructure.Persistence.Common.Repositories;

/// <summary>
/// Generic base repository for Audit Trail entities shared across all persistence providers.
/// </summary>
/// <typeparam name="TDbContext">The DbContext type to use</typeparam>
public abstract class BaseAuditRepository<TDbContext> : IAuditRepository
    where TDbContext : BaseDbContext
{
    protected readonly IDbContextFactory<TDbContext> ContextFactory;

    protected BaseAuditRepository(IDbContextFactory<TDbContext> appContextFactory)
    {
        ContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public virtual async Task<(IReadOnlyList<AuditTrail> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);

        var totalCount = await context.AuditLogs
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = await context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public virtual async Task<AuditTrail?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task AddAsync(AuditTrail auditTrail, CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await context.AuditLogs.AddAsync(auditTrail, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
