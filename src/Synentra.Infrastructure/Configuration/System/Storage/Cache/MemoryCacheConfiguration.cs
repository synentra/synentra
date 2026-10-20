namespace Synentra.Infrastructure.Configuration.System.Storage.Cache;

public class MemoryCacheConfiguration
{
    public TimeSpan? TimeToLive { get; set; } = TimeSpan.FromHours(24);
}