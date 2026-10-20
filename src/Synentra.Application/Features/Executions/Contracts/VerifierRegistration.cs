namespace Synentra.Application.Features.Executions.Contracts;

public sealed record VerifierRegistration(
    string VerifierRef, 
    string Version, 
    IReadOnlyDictionary<string, string> Configuration);
