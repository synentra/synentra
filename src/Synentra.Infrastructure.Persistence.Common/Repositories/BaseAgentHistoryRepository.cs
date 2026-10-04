using Microsoft.EntityFrameworkCore;
using Synentra.Application.Abstractions.Persistence;
using Synentra.Application.Models;
using Synentra.Domain.Agents;

namespace Synentra.Infrastructure.Persistence.Common.Repositories;

/// <summary>
/// Generic base repository for Agent History entities shared across all persistence providers.
/// </summary>
/// <typeparam name="TDbContext">The DbContext type to use</typeparam>
public abstract class BaseAgentHistoryRepository<TDbContext> : IAgentHistoryRepository
    where TDbContext : BaseDbContext
{
    protected readonly IDbContextFactory<TDbContext> ContextFactory;

    protected BaseAgentHistoryRepository(IDbContextFactory<TDbContext> appContextFactory)
    {
        ContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public virtual async Task<AgentStats?> GetStatsAsync(Guid agentId, TimeSpan window, CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);

        var start = DateTime.UtcNow - window;
        var histories = await context.AgentHistories
            .Where(h => h.AgentId == agentId && h.WindowStart >= start)
            .ToListAsync(cancellationToken);

        if (histories.Count == 0) return null;

        var totalRequests = histories.Sum(h => h.TotalRequests);
        var violationCount = histories.Sum(h => h.ViolationCount);
        var minutes = window.TotalMinutes;
        var requestsPerMinute = totalRequests / minutes;

        return new AgentStats
        {
            AgentId = agentId,
            TotalRequests = totalRequests,
            ViolationCount = violationCount,
            CurrentRequestsPerMinute = requestsPerMinute,
            SeenEndpoints = new HashSet<string>(),
            ActiveHours = new HashSet<int>()
        };
    }

    public virtual async Task<AgentBaseline?> GetBaselineAsync(Guid agentId, TimeSpan lookback, CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        var start = DateTime.UtcNow - lookback;
        var histories = await context.AgentHistories
            .Where(h => h.AgentId == agentId && h.WindowStart >= start)
            .ToListAsync(cancellationToken);

        if (histories.Count == 0) return null;

        var totalMinutes = (DateTime.UtcNow - start).TotalMinutes;
        var avgRequestsPerMinute = histories.Sum(h => h.TotalRequests) / totalMinutes;
        var avgViolationRate = histories.Sum(h => h.ViolationCount) / (double)histories.Sum(h => h.TotalRequests);

        // For endpoints and hours, you need separate tables; for now, return empty
        return new AgentBaseline
        {
            AverageRequestsPerMinute = avgRequestsPerMinute,
            AverageViolationRate = avgViolationRate,
            FrequentEndpoints = new HashSet<string>(),
            TypicalActiveHours = new HashSet<int>()
        };
    }

    public virtual async Task<AgentHistory?> GetRecentAsync(Guid agentId, TimeSpan window, CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);

        var start = DateTime.UtcNow - window;
        var histories = await context.AgentHistories
            .Where(h => h.AgentId == agentId && h.WindowStart >= start)
            .ToListAsync(cancellationToken);

        if (histories.Count == 0) return null;

        return new AgentHistory
        {
            AgentId = agentId,
            TotalRequests = histories.Sum(h => h.TotalRequests),
            ViolationCount = histories.Sum(h => h.ViolationCount),
            WindowStart = start
        };
    }

    public virtual async Task RecordRequestAsync(Guid agentId, bool wasViolation, double riskScore, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var windowStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
        var windowDuration = 60; // seconds

        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        var history = await context.AgentHistories
            .FirstOrDefaultAsync(h => h.AgentId == agentId && h.WindowStart == windowStart, cancellationToken);

        if (history == null)
        {
            history = new AgentHistory
            {
                Id = Guid.NewGuid(),
                AgentId = agentId,
                WindowStart = windowStart,
                WindowDurationSeconds = windowDuration,
                TotalRequests = 1,
                ViolationCount = wasViolation ? 1 : 0,
                AverageRiskScore = riskScore
            };
            context.AgentHistories.Add(history);
        }
        else
        {
            history.TotalRequests++;
            if (wasViolation) history.ViolationCount++;
            // Update average risk score (weighted)
            history.AverageRiskScore = (history.AverageRiskScore * (history.TotalRequests - 1) + riskScore) / history.TotalRequests;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
