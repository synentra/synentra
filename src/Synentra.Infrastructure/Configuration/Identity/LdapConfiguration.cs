namespace Synentra.Infrastructure.Configuration.Identity;

public class LdapConfiguration
{
    public string Host { get; set; } = string.Empty;
    public string BaseDn { get; set; } = string.Empty;
    public string UserDnPattern { get; set; } = string.Empty;
    public string BindDn { get; set; } = string.Empty;
    public string? BindPassword { get; set; } = string.Empty;
}