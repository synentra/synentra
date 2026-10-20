namespace Synentra.Application.Features.Manifests.Contracts;

public sealed record ValidationIssue(
    string Severity, 
    string Code, 
    string Message, 
    string? Path = null);
