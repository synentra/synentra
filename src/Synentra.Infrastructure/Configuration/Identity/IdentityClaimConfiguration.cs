namespace Synentra.Infrastructure.Configuration.Identity;

public class IdentityClaimConfiguration
{
    public string AgentId { get; set; } = "sub";
    public string SecurityScope { get; set; } = "security_scope";
    public string Roles { get; set; } = "roles";
}