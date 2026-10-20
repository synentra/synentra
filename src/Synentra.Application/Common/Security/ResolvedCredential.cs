namespace Synentra.Application.Common.Security;

public sealed record ResolvedCredential(
    string Type, 
    string Value, 
    DateTimeOffset? ExpiresAt, 
    string Scheme = "Bearer")
{
    public System.Net.Http.Headers.AuthenticationHeaderValue ToAuthHeader() => new(Scheme, Value);
}
