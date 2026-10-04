using Microsoft.EntityFrameworkCore;
using Synentra.Application.Abstractions.Persistence;
using Synentra.Domain.Agents;

namespace Synentra.Infrastructure.Persistence.Common.Repositories;

/// <summary>
/// Generic base repository for Agent entities shared across all persistence providers.
/// </summary>
/// <typeparam name="TDbContext">The DbContext type to use</typeparam>
public abstract class BaseAgentRepository<TDbContext> : IAgentRepository
    where TDbContext : BaseDbContext
{
    protected readonly IDbContextFactory<TDbContext> ContextFactory;

    protected BaseAgentRepository(IDbContextFactory<TDbContext> appContextFactory)
    {
        ContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public virtual async Task<(IReadOnlyList<Agent> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);

        var totalCount = await context.Agents
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = await context.Agents
            .OrderBy(a => a.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public virtual async Task<IReadOnlyList<Agent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Agents.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<Agent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Agents.FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task AddAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Agents.AddAsync(agent, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task UpdateAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        context.Agents.Update(agent);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        var agent = await context.Agents.FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false) 
            ?? throw new InvalidOperationException($"Agent with ID {id} does not exist.");

        context.Agents.Remove(agent);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
