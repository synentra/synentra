using Synentra.Infrastructure.Configuration.Security.AgentAuth;

namespace Synentra.Infrastructure.Configuration.Security;

public class SecurityConfiguration
{
    public AgentAuthConfiguration AgentAuth { get; set; } = new();
}