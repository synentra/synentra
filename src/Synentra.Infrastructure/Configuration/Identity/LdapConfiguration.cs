namespace Synentra.Infrastructure.Configuration.Identity;

public class LdapConfiguration
{
    public string Endpoint { get; set; } = "";
    public string BaseDn { get; set; } = "";
    public string BindDn { get; set; } = "";
    public string BindPasswordSecretRef { get; set; } = "";
    public string AdminGroupDn { get; set; } = "";
    public string ReviewerGroupDn { get; set; } = "";
}