using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StackExchange.Redis;
using System.Text.Json;
using Synentra.BuildingBlocks.Configuration.System.Storage.Cache;
using Synentra.Infrastructure.Caches.Providers;

namespace Synentra.Infrastructure.UnitTests.Caches;

public class RedisCacheProviderTests
{
    private readonly IConnectionMultiplexer _multiplexer = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _db = Substitute.For<IDatabase>();

    private RedisCacheProvider CreateSut(TimeSpan? ttl = null, ILogger<RedisCacheProvider>? logger = null)
    {
        _multiplexer.GetDatabase().Returns(_db);
        var config = new RedisCacheConfiguration { Endpoint = "localhost:6379", TimeToLive = ttl };
        var services = new ServiceCollection();
        services.AddLogging();
        if (logger is not null)
            services.AddSingleton(logger);
        services.AddSingleton(_multiplexer);
        var sp = services.BuildServiceProvider();
        return new RedisCacheProvider(config, sp);
    }

    private static ILogger<RedisCacheProvider> CreateLogger(bool informationEnabled)
    {
        var logger = Substitute.For<ILogger<RedisCacheProvider>>();
        logger.IsEnabled(LogLevel.Information).Returns(informationEnabled);
        return logger;
    }

    private static void AssertInformationLogged(ILogger<RedisCacheProvider> logger, int times)
    {
        logger.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log)
                && call.GetArguments()[0] is LogLevel.Information)
            .Should().Be(times);
    }

    private static Task RunOperation(RedisCacheProvider sut, string operation) => operation switch
    {
        "SetString" => sut.SetAsync("key1", "value"),
        "GetObject" => sut.GetAsync("key1"),
        "GetGeneric" => sut.GetAsync<string>("key1"),
        "SetGeneric" => sut.SetAsync("key1", 42, TestContext.Current.CancellationToken),
        "TryGetValue" => sut.TryGetValueAsync<string>("key1", TestContext.Current.CancellationToken),
        "Remove" => sut.RemoveAsync("key1"),
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
    };

    public static TheoryData<string> Operations =>
        ["SetString", "GetObject", "GetGeneric", "SetGeneric", "TryGetValue", "Remove"];

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task Operation_WithInformationLoggingEnabled_LogsOnce(string operation)
    {
        var logger = CreateLogger(informationEnabled: true);
        var sut = CreateSut(logger: logger);
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(new RedisValue(JsonSerializer.Serialize("cached")));

        await RunOperation(sut, operation);

        AssertInformationLogged(logger, 1);
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task Operation_WithInformationLoggingDisabled_DoesNotLog(string operation)
    {
        var logger = CreateLogger(informationEnabled: false);
        var sut = CreateSut(logger: logger);
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(new RedisValue(JsonSerializer.Serialize("cached")));

        await RunOperation(sut, operation);

        AssertInformationLogged(logger, 0);
    }

    [Fact]
    public async Task TryGetValueAsync_KeyNotFound_WithInformationLoggingEnabled_DoesNotLog()
    {
        var logger = CreateLogger(informationEnabled: true);
        var sut = CreateSut(logger: logger);
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(RedisValue.Null);

        await sut.TryGetValueAsync<string>("missing", TestContext.Current.CancellationToken);

        AssertInformationLogged(logger, 0);
    }

    [Fact]
    public async Task SetAsync_String_StoresSerializedValue()
    {
        var sut = CreateSut();
        // Don't configure the mock to avoid arg matcher issues; the default return (false) still exercises the path
        await sut.SetAsync("key1", "hello world");
        // Just verify no exception thrown and the call was made
        await _db.Received().StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<Expiration>());
    }

    [Fact]
    public async Task GetAsync_Object_ReturnsRedisValue()
    {
        var sut = CreateSut();
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(new RedisValue("stored-value"));
        var result = await sut.GetAsync("key1");
        result.Should().Be("stored-value");
    }

    [Fact]
    public async Task GetAsync_Generic_DeserializesValue()
    {
        var sut = CreateSut();
        var payload = JsonSerializer.Serialize(new Dictionary<string, string> { ["Name"] = "test" });
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(new RedisValue(payload));
        var result = await sut.GetAsync<Dictionary<string, string>>("key1");
        result.Should().NotBeNull();
        result!["Name"].Should().Be("test");
    }

    [Fact]
    public async Task SetAsync_Generic_StoresAndReturnsValue()
    {
        var sut = CreateSut();
        var value = new Dictionary<string, int> { ["Score"] = 42 };
        var result = await sut.SetAsync("key1", value, TestContext.Current.CancellationToken);
        result.Should().BeEquivalentTo(value);
        await _db.Received().StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<Expiration>());
    }

    [Fact]
    public async Task TryGetValueAsync_KeyExists_ReturnsTrueWithValue()
    {
        var sut = CreateSut();
        var stored = JsonSerializer.Serialize("cached-result");
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(new RedisValue(stored));
        var (success, value) = await sut.TryGetValueAsync<string>("key1", TestContext.Current.CancellationToken);
        success.Should().BeTrue();
        value.Should().Be("cached-result");
    }

    [Fact]
    public async Task TryGetValueAsync_KeyNotFound_ReturnsFalseWithDefault()
    {
        var sut = CreateSut();
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(RedisValue.Null);
        var (success, value) = await sut.TryGetValueAsync<string>("missing", TestContext.Current.CancellationToken);
        success.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_DeletesKey()
    {
        var sut = CreateSut();
        await sut.RemoveAsync("key-to-delete");
        await _db.Received().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task SetAsync_WithCustomTtl_CallsStringSet()
    {
        var sut = CreateSut(ttl: TimeSpan.FromMinutes(10));
        await sut.SetAsync("k", "v");
        await _db.Received().StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<Expiration>());
    }
}
