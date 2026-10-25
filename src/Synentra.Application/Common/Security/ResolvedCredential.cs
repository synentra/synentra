namespace Synentra.Application.Common.Security;

public sealed record ResolvedCredential(
    string Type,
    [property: System.Text.Json.Serialization.JsonIgnore] string Value, 
    DateTimeOffset? ExpiresAt, 
    string Scheme = "Bearer")
{
    // Prevent accidental leakage through structured logging or exception interpolation.
    public override string ToString() => "ResolvedCredential([REDACTED])";
    public System.Net.Http.Headers.AuthenticationHeaderValue ToAuthHeader() => new(Scheme, Value);
}
