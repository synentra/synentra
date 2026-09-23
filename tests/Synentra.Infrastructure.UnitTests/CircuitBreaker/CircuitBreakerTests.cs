using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Synentra.BuildingBlocks.Configuration.System;
using Synentra.BuildingBlocks.Configuration.System.CircuitBreaker;
using Synentra.Infrastructure.CircuitBreaker;

namespace Synentra.Infrastructure.UnitTests.CircuitBreaker;

public class CircuitBreakerTests
{
    private readonly IOptions<SystemConfiguration> _options = Substitute.For<IOptions<SystemConfiguration>>();
    private readonly ILogger<Synentra.Infrastructure.CircuitBreaker.CircuitBreaker> _logger = Substitute.For<ILogger<Synentra.Infrastructure.CircuitBreaker.CircuitBreaker>>();
    private readonly Synentra.Infrastructure.CircuitBreaker.CircuitBreaker _circuitBreaker;

    public CircuitBreakerTests()
    {
        _options.Value.Returns(new SystemConfiguration
        {
            CircuitBreaker = new CircuitBreakerConfiguration
            {
                Enabled = true,
                FailureThreshold = 3,
                OpenDurationSeconds = 10,
                SamplingWindowSeconds = 30
            }
        });
        _logger.IsEnabled(LogLevel.Information).Returns(true);
        _logger.IsEnabled(LogLevel.Debug).Returns(true);
        _circuitBreaker = new Synentra.Infrastructure.CircuitBreaker.CircuitBreaker(_options, _logger);
    }

    [Fact]
    public void IsAllowed_WhenDisabled_ShouldReturnTrue()
    {
        // Arrange
        _options.Value.Returns(new SystemConfiguration { CircuitBreaker = new CircuitBreakerConfiguration { Enabled = false } });
        var cb = new Synentra.Infrastructure.CircuitBreaker.CircuitBreaker(_options, _logger);

        // Act
        var isAllowed = cb.IsAllowed("test-host");

        // Assert
        isAllowed.Should().BeTrue();
    }

    [Fact]
    public void IsAllowed_WhenClosed_ShouldReturnTrue()
    {
        // Act
        var isAllowed = _circuitBreaker.IsAllowed("test-host");

        // Assert
        isAllowed.Should().BeTrue();
    }

    [Fact]
    public void RecordFailure_BelowThreshold_ShouldRemainClosed()
    {
        // Act
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");

        // Assert
        _circuitBreaker.IsAllowed("test-host").Should().BeTrue();
    }

    [Fact]
    public void RecordSuccess_WhenOpen_ShouldCloseCircuit()
    {
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");

        _circuitBreaker.RecordSuccess("test-host");

        _circuitBreaker.IsAllowed("test-host").Should().BeTrue();
    }

    [Fact]
    public void RecordFailure_AtThreshold_ShouldOpen()
    {
        // Act
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");

        // Assert
        _circuitBreaker.IsAllowed("test-host").Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_WhenOpen_ShouldReturnFalse()
    {
        // Arrange
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");

        // Act
        var isAllowed = _circuitBreaker.IsAllowed("test-host");

        // Assert
        isAllowed.Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_WhenHalfOpen_ShouldReturnTrueForProbe()
    {
        // Arrange
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");
        SetOpenedAt("test-host", DateTime.UtcNow.AddSeconds(-11));

        // Act
        var isAllowed = _circuitBreaker.IsAllowed("test-host");

        // Assert
        isAllowed.Should().BeTrue();
    }

    [Fact]
    public void RecordSuccess_WhenHalfOpen_ShouldCloseCircuit()
    {
        // Arrange
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");
        SetOpenedAt("test-host", DateTime.UtcNow.AddSeconds(-11));
        _circuitBreaker.IsAllowed("test-host"); // Probe

        // Act
        _circuitBreaker.RecordSuccess("test-host");

        // Assert
        _circuitBreaker.IsAllowed("test-host").Should().BeTrue();
    }

    [Fact]
    public void RecordFailure_ResetsAfterWindow()
    {
        // Arrange
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");
        SetWindowStart("test-host", DateTime.UtcNow.AddSeconds(-31));

        _circuitBreaker.RecordFailure("test-host");

        // Assert
        _circuitBreaker.IsAllowed("test-host").Should().BeTrue();
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new Synentra.Infrastructure.CircuitBreaker.CircuitBreaker(null!, NullLogger<Synentra.Infrastructure.CircuitBreaker.CircuitBreaker>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new Synentra.Infrastructure.CircuitBreaker.CircuitBreaker(_options, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordSuccess_WhenDisabled_DoesNotThrow()
    {
        _options.Value.Returns(new SystemConfiguration { CircuitBreaker = new CircuitBreakerConfiguration { Enabled = false } });
        var cb = new Synentra.Infrastructure.CircuitBreaker.CircuitBreaker(_options, _logger);

        var act = () => cb.RecordSuccess("test-host");

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordFailure_WhenDisabled_DoesNotThrow()
    {
        _options.Value.Returns(new SystemConfiguration { CircuitBreaker = new CircuitBreakerConfiguration { Enabled = false } });
        var cb = new Synentra.Infrastructure.CircuitBreaker.CircuitBreaker(_options, _logger);

        var act = () => cb.RecordFailure("test-host");

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordFailure_WhenAlreadyOpen_DoesNotResetOpenedAt()
    {
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");

        var openedAtBefore = GetOpenedAt("test-host");
        _circuitBreaker.RecordFailure("test-host");
        var openedAtAfter = GetOpenedAt("test-host");

        openedAtAfter.Should().Be(openedAtBefore);
        _circuitBreaker.IsAllowed("test-host").Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_WhenAlreadyHalfOpen_ShouldReturnTrue()
    {
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");
        _circuitBreaker.RecordFailure("test-host");
        SetOpenedAt("test-host", DateTime.UtcNow.AddSeconds(-11));

        _circuitBreaker.IsAllowed("test-host").Should().BeTrue(); // transition Open -> HalfOpen
        _circuitBreaker.IsAllowed("test-host").Should().BeTrue(); // HalfOpen probe path
    }

    [Fact]
    public void RecordSuccess_WhenOpen_WithInformationDisabled_ClosesWithoutThrowing()
    {
        var options = Substitute.For<IOptions<SystemConfiguration>>();
        options.Value.Returns(new SystemConfiguration
        {
            CircuitBreaker = new CircuitBreakerConfiguration
            {
                Enabled = true,
                FailureThreshold = 3,
                OpenDurationSeconds = 10,
                SamplingWindowSeconds = 30
            }
        });

        var logger = Substitute.For<ILogger<Synentra.Infrastructure.CircuitBreaker.CircuitBreaker>>();
        logger.IsEnabled(LogLevel.Information).Returns(false);
        logger.IsEnabled(LogLevel.Debug).Returns(true);

        var cb = new Synentra.Infrastructure.CircuitBreaker.CircuitBreaker(options, logger);
        cb.RecordFailure("test-host");
        cb.RecordFailure("test-host");
        cb.RecordFailure("test-host");

        var act = () => cb.RecordSuccess("test-host");

        act.Should().NotThrow();
        cb.IsAllowed("test-host").Should().BeTrue();
    }

    [Fact]
    public void Constructor_WhenDisabled_WithInformationLoggingEnabled_ExecutesDisabledLoggingBranch()
    {
        _options.Value.Returns(new SystemConfiguration
        {
            CircuitBreaker = new CircuitBreakerConfiguration
            {
                Enabled = false,
                FailureThreshold = 3,
                OpenDurationSeconds = 10,
                SamplingWindowSeconds = 30
            }
        });

        var act = () => new Synentra.Infrastructure.CircuitBreaker.CircuitBreaker(_options, _logger);

        act.Should().NotThrow();
    }

    private void SetOpenedAt(string host, DateTime value)
    {
        var circuit = GetHostCircuit(host);
        var openedAtField = circuit.GetType().GetField("OpenedAt")!;
        openedAtField.SetValue(circuit, value);
    }

    private DateTime GetOpenedAt(string host)
    {
        var circuit = GetHostCircuit(host);
        var openedAtField = circuit.GetType().GetField("OpenedAt")!;
        return (DateTime)openedAtField.GetValue(circuit)!;
    }

    private void SetWindowStart(string host, DateTime value)
    {
        var circuit = GetHostCircuit(host);
        var windowStartField = circuit.GetType().GetField("WindowStart")!;
        windowStartField.SetValue(circuit, value);
    }

    private object GetHostCircuit(string host)
    {
        var circuitsField = typeof(Synentra.Infrastructure.CircuitBreaker.CircuitBreaker)
            .GetField("_circuits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var circuits = circuitsField.GetValue(_circuitBreaker)!;
        var tryGetValue = circuits.GetType().GetMethod("TryGetValue")!;
        var args = new object?[] { host, null };
        var found = (bool)tryGetValue.Invoke(circuits, args)!;

        found.Should().BeTrue();
        return args[1]!;
    }
}
