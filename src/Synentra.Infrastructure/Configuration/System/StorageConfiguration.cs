using Synentra.Infrastructure.Configuration.System.Storage.Cache;
using Synentra.Infrastructure.Configuration.System.Storage.Database;

namespace Synentra.Infrastructure.Configuration.System;

public class StorageConfiguration
{
    public DatabaseConfiguration Database { get; set; } = new();
    public CacheConfiguration Cache { get; set; } = new();
}