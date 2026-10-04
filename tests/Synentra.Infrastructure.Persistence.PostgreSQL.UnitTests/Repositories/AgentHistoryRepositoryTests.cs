using Microsoft.EntityFrameworkCore;
using Synentra.Domain.Agents;
using Synentra.Infrastructure.Persistence.PostgreSQL.Contexts;
using Synentra.Infrastructure.Persistence.PostgreSQL.Repositories;
using Synentra.Infrastructure.Persistence.PostgreSQL.UnitTests.Helpers;

namespace Synentra.Infrastructure.Persistence.PostgreSQL.UnitTests.Repositories;

public class AgentHistoryRepositoryTests
{
    private static IDbContextFactory<PostgresqlApplicationContext> CreateFactory(string dbName)
        => PostgresqlTestContextFactory.CreateFactory(dbName);

    private static async Task<Agent> SeedAgentAsync(string dbName)
    {
        var agent = new Agent("TestAgent", "owner-1", "hash");
        await using var ctx = PostgresqlTestContextFactory.Create(dbName);
        ctx.Agents.Add(agent);
        await ctx.SaveChangesAsync();
        return agent;
    }

    private static async Task SeedHistoryAsync(string dbName, Guid agentId, int totalRequests, int violationCount, double riskScore, DateTime windowStart)
    {
        await using var ctx = PostgresqlTestContextFactory.Create(dbName);
        ctx.AgentHistories.Add(new AgentHistory
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            WindowStart = windowStart,
            WindowDurationSeconds = 60,
            TotalRequests = totalRequests,
            ViolationCount = violationCount,
            AverageRiskScore = riskScore
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        Action act = () => new AgentHistoryRepository(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("appContextFactory");
    }

    [Fact]
    public async Task GetStatsAsync_NoHistories_ReturnsNull()
    {
        var factory = CreateFactory(Guid.NewGuid().ToString());
        var repo = new AgentHistoryRepository(factory);

        var result = await repo.GetStatsAsync(Guid.NewGuid(), TimeSpan.FromHours(1), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStatsAsync_WithHistories_ReturnsAggregatedStats()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);
        var now = DateTime.UtcNow;

        await SeedHistoryAsync(dbName, agent.Id, 10, 2, 0.3, now.AddMinutes(-10));
        await SeedHistoryAsync(dbName, agent.Id, 20, 5, 0.5, now.AddMinutes(-5));

        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        var stats = await repo.GetStatsAsync(agent.Id, TimeSpan.FromHours(1), CancellationToken.None);

        stats.Should().NotBeNull();
        stats!.AgentId.Should().Be(agent.Id);
        stats.TotalRequests.Should().Be(30);
        stats.ViolationCount.Should().Be(7);
        stats.CurrentRequestsPerMinute.Should().BeApproximately(30.0 / 60, 0.01);
    }

    [Fact]
    public async Task GetStatsAsync_HistoriesOutsideWindow_ReturnsNull()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);

        await SeedHistoryAsync(dbName, agent.Id, 10, 2, 0.3, DateTime.UtcNow.AddHours(-5));

        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        var result = await repo.GetStatsAsync(agent.Id, TimeSpan.FromMinutes(30), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBaselineAsync_NoHistories_ReturnsNull()
    {
        var factory = CreateFactory(Guid.NewGuid().ToString());
        var repo = new AgentHistoryRepository(factory);

        var result = await repo.GetBaselineAsync(Guid.NewGuid(), TimeSpan.FromHours(24), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBaselineAsync_WithHistories_ReturnsBaseline()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);
        var now = DateTime.UtcNow;

        await SeedHistoryAsync(dbName, agent.Id, 100, 10, 0.3, now.AddHours(-6));
        await SeedHistoryAsync(dbName, agent.Id, 120, 12, 0.35, now.AddHours(-3));

        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        var baseline = await repo.GetBaselineAsync(agent.Id, TimeSpan.FromHours(12), CancellationToken.None);

        baseline.Should().NotBeNull();
        baseline!.AverageRequestsPerMinute.Should().BeGreaterThan(0);
        baseline.AverageViolationRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetRecentAsync_WithHistories_ReturnsAggregatedRecent()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);
        var now = DateTime.UtcNow;

        await SeedHistoryAsync(dbName, agent.Id, 15, 3, 0.25, now.AddMinutes(-5));
        await SeedHistoryAsync(dbName, agent.Id, 25, 5, 0.4, now.AddMinutes(-2));

        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        var recent = await repo.GetRecentAsync(agent.Id, TimeSpan.FromMinutes(10), CancellationToken.None);

        recent.Should().NotBeNull();
        recent!.TotalRequests.Should().Be(40);
        recent.ViolationCount.Should().Be(8);
    }

    [Fact]
    public async Task GetRecentAsync_NoHistories_ReturnsNull()
    {
        var factory = CreateFactory(Guid.NewGuid().ToString());
        var repo = new AgentHistoryRepository(factory);

        var result = await repo.GetRecentAsync(Guid.NewGuid(), TimeSpan.FromMinutes(30), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RecordRequestAsync_CreatesNewHistory()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);
        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        await repo.RecordRequestAsync(agent.Id, false, 0.1);

        await using var ctx = PostgresqlTestContextFactory.Create(dbName);
        var history = await ctx.AgentHistories.FirstOrDefaultAsync(h => h.AgentId == agent.Id);

        history.Should().NotBeNull();
        history!.TotalRequests.Should().Be(1);
        history.ViolationCount.Should().Be(0);
    }

    [Fact]
    public async Task RecordRequestAsync_UpdatesExistingHistory()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);
        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        await repo.RecordRequestAsync(agent.Id, false, 0.1);
        await repo.RecordRequestAsync(agent.Id, true, 0.8);

        await using var ctx = PostgresqlTestContextFactory.Create(dbName);
        var history = await ctx.AgentHistories.FirstOrDefaultAsync(h => h.AgentId == agent.Id);

        history.Should().NotBeNull();
        history!.TotalRequests.Should().Be(2);
        history.ViolationCount.Should().Be(1);
    }
}
