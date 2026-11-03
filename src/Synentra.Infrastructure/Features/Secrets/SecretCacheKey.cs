namespace Synentra.Infrastructure.Features.Secrets;

public readonly record struct SecretCacheKey(
    string Provider,
    string Path,
    string Version,
    long Generation,
    string BindingId,
    string Principal,
    string Scope,
    string Target,
    string Operation);