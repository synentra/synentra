namespace Synentra.Infrastructure.Configuration.Identity;

public class LdapConfiguration
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 636;
    public string BaseDn { get; set; } = "";
    public string ServiceDn { get; set; } = "";
    public string ServicePassword { get; set; } = "";
    public string AdminGroupDn { get; set; } = "";
    public string ReviewerGroupDn { get; set; } = "";
}