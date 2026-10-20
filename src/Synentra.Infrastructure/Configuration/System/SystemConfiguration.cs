using Synentra.Infrastructure.Configuration.System.CircuitBreaker;
using Synentra.Infrastructure.Configuration.System.Cors;
using Synentra.Infrastructure.Configuration.System.RateLimit;
using Synentra.Infrastructure.Configuration.System.Server;

namespace Synentra.Infrastructure.Configuration.System;

public class SystemConfiguration
{
    public ServerConfiguration Server { get; set; } = new();
    public StorageConfiguration Storage { get; set; } = new();
    public RateLimitConfiguration RateLimit { get; set; } = new();
    public CircuitBreakerConfiguration CircuitBreaker { get; set; } = new();
    public CorsConfiguration Cors { get; set; } = new();
}