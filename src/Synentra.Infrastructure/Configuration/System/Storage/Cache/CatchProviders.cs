namespace Synentra.Infrastructure.Configuration.System.Storage.Cache;

public class CacheProviders
{
    public RedisCacheConfiguration Redis { get; set; } = new();
    public MemoryCacheConfiguration Memory { get; set; } = new();
}