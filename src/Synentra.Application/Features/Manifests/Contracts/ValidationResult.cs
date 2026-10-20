namespace Synentra.Application.Features.Manifests.Contracts;

public sealed record ValidationResult(bool IsValid, IReadOnlyList<ValidationIssue> Issues)
{
    public static ValidationResult Ok() => new(true, Array.Empty<ValidationIssue>());
}
