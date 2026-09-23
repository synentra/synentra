using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Synentra.BuildingBlocks.Configuration.System;
using Synentra.BuildingBlocks.Configuration.System.RateLimit;
using Synentra.Infrastructure.RateLimit;

namespace Synentra.Infrastructure.UnitTests.RateLimit;

public class AgentRateLimiterTests
{
    private static AgentRateLimiter CreateSut(bool enabled = true, int requestsPerMinute = 5)
    {
        var config = new SystemConfiguration
        {
            RateLimit = new RateLimitConfiguration
            {
                Enabled = enabled,
                DefaultRequestsPerMinute = requestsPerMinute
            }
        };
        return new AgentRateLimiter(Options.Create(config), NullLogger<AgentRateLimiter>.Instance);
    }

    [Fact]
    public async Task IsAllowedAsync_WhenDisabled_AlwaysReturnsTrue()
    {
        var sut = CreateSut(enabled: false, requestsPerMinute: 1);
        var agentId = Guid.NewGuid();

        for (int i = 0; i < 10; i++)
        {
            var result = await sut.IsAllowedAsync(agentId, TestContext.Current.CancellationToken);
            result.Should().BeTrue();
        }
    }

    [Fact]
    public async Task IsAllowedAsync_WithinLimit_ReturnsTrue()
    {
        var sut = CreateSut(requestsPerMinute: 5);
        var agentId = Guid.NewGuid();

        for (int i = 0; i < 5; i++)
        {
            var result = await sut.IsAllowedAsync(agentId, TestContext.Current.CancellationToken);
            result.Should().BeTrue($"request {i + 1} should be allowed");
        }
    }

    [Fact]
    public async Task IsAllowedAsync_ExceedsLimit_ReturnsFalse()
    {
        var sut = CreateSut(requestsPerMinute: 3);
        var agentId = Guid.NewGuid();

        for (int i = 0; i < 3; i++)
            await sut.IsAllowedAsync(agentId, TestContext.Current.CancellationToken);

        var result = await sut.IsAllowedAsync(agentId, TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_DifferentAgents_HaveSeparateWindows()
    {
        var sut = CreateSut(requestsPerMinute: 2);
        var agent1 = Guid.NewGuid();
        var agent2 = Guid.NewGuid();

        await sut.IsAllowedAsync(agent1, TestContext.Current.CancellationToken);
        await sut.IsAllowedAsync(agent1, TestContext.Current.CancellationToken);
        // agent1 is exhausted
        (await sut.IsAllowedAsync(agent1, TestContext.Current.CancellationToken)).Should().BeFalse();

        // agent2 is independent
        (await sut.IsAllowedAsync(agent2, TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new AgentRateLimiter(null!, NullLogger<AgentRateLimiter>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var config = Options.Create(new SystemConfiguration
        {
            RateLimit = new RateLimitConfiguration
            {
                Enabled = true,
                DefaultRequestsPerMinute = 5
            }
        });

        var act = () => new AgentRateLimiter(config, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task IsAllowedAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.IsAllowedAsync(Guid.NewGuid(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task IsAllowedAsync_WithLoggingEnabled_ExecutesDebugAndWarningBranches()
    {
        var logger = Substitute.For<ILogger<AgentRateLimiter>>();
        logger.IsEnabled(LogLevel.Information).Returns(true);
        logger.IsEnabled(LogLevel.Debug).Returns(true);

        var config = new SystemConfiguration
        {
            RateLimit = new RateLimitConfiguration
            {
                Enabled = true,
                DefaultRequestsPerMinute = 2
            }
        };

        var sut = new AgentRateLimiter(Options.Create(config), logger);
        var agentId = Guid.NewGuid();

        (await sut.IsAllowedAsync(agentId, TestContext.Current.CancellationToken)).Should().BeTrue();
        (await sut.IsAllowedAsync(agentId, TestContext.Current.CancellationToken)).Should().BeTrue();
        (await sut.IsAllowedAsync(agentId, TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public void Constructor_WhenDisabled_WithInformationLoggingEnabled_ExecutesDisabledLoggingBranch()
    {
        var logger = Substitute.For<ILogger<AgentRateLimiter>>();
        logger.IsEnabled(LogLevel.Information).Returns(true);

        var config = Options.Create(new SystemConfiguration
        {
            RateLimit = new RateLimitConfiguration
            {
                Enabled = false,
                DefaultRequestsPerMinute = 1
            }
        });

        var act = () => new AgentRateLimiter(config, logger);

        act.Should().NotThrow();
    }
}
