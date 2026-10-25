using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.Extensions.Options;
using Synentra.Application.Common.Security;
using Synentra.Domain.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Configuration.Identity;
using Synentra.Infrastructure.Configuration.Security;

namespace Synentra.Infrastructure.Features.Identity;

public sealed class LdapPrincipalResolver(IOptions<IdentityConfiguration> options) : IPrincipalResolver
{
    public string ResolverId => "ldap";
    public string ProviderKind => "ldap";

    public Task<CanonicalPrincipal> ResolveAsync(string credential, CancellationToken ct) => Task.Run(() =>
    {
        ct.ThrowIfCancellationRequested();
        var parts = credential.Split(':', 2);

        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrEmpty(parts[1]))
            throw new UnauthorizedAccessException("Invalid LDAP credential.");

        var o = options.Value.Providers.Ldap;
        using var lookup = Connect(o);
        lookup.Bind(new NetworkCredential(o.ServiceDn, o.ServicePassword));
        var response = (SearchResponse)lookup
                        .SendRequest(new SearchRequest(
                            o.BaseDn, 
                            "(&(objectClass=person)(uid=" + Escape(parts[0]) + "))", 
                            SearchScope.Subtree, 
                            "uid", 
                            "securityScope", 
                            "memberOf")
                        );

        if (response.Entries.Count != 1)
            throw new UnauthorizedAccessException("LDAP identity not uniquely resolved.");

        var entry = response.Entries[0];
        using var user = Connect(o);
        user.Bind(new NetworkCredential(entry.DistinguishedName, parts[1]));
        ct.ThrowIfCancellationRequested();
        string Required(string name) => entry.Attributes[name] is { Count: > 0 } a 
            ? a[0].ToString()! 
            : throw new UnauthorizedAccessException("Required LDAP attribute missing.");

        var groups = entry.Attributes["memberOf"]?
                    .GetValues(typeof(string))
                    .Cast<string>()
                    .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();

        var type = !string.IsNullOrEmpty(o.AdminGroupDn) && groups.Contains(o.AdminGroupDn) 
            ? PrincipalType.Admin 
            : !string.IsNullOrEmpty(o.ReviewerGroupDn) && groups.Contains(o.ReviewerGroupDn) ? PrincipalType.Reviewer : PrincipalType.Agent;

        return new CanonicalPrincipal(
            Required("uid"), 
            Required("securityScope"), 
            "ldaps://" + o.Host, 
            entry.DistinguishedName,
            groups, new Dictionary<string, string>(), type);
    }, ct);

    private static LdapConnection Connect(LdapConfiguration o)
    {
        var connection = new LdapConnection(new LdapDirectoryIdentifier(o.Host, o.Port))
        {
            AuthType = AuthType.Basic,
            Timeout = TimeSpan.FromSeconds(10)
        };
        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.SecureSocketLayer = true;
        connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
        return connection; // Platform certificate validation is retained.
    }

    private static string Escape(string input) => input.Replace("\\", "\\5c").Replace("*", "\\2a").Replace("(", "\\28").Replace(")", "\\29").Replace("\0", "\\00");
}
