namespace Synentra.Application.Features.Effects.Contracts;

public sealed record EffectRegistration(
    string ResolverRef, 
    string Version, 
    IReadOnlyDictionary<string, string> Configuration);
