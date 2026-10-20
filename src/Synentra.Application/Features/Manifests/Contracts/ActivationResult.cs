namespace Synentra.Application.Features.Manifests.Contracts;

public sealed record ActivationResult(
    bool Activated, 
    string? PreviousRevision, 
    string NewRevision, 
    IReadOnlyList<ValidationIssue> Issues);
