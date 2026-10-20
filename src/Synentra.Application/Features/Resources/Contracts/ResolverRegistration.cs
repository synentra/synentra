namespace Synentra.Application.Features.Resources.Contracts;

public sealed record ResolverRegistration(
    string Role, 
    string ResolverRef, 
    string Version, 
    IReadOnlyDictionary<string, string> Configuration);
