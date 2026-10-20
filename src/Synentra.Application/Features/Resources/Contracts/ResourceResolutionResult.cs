using Synentra.Domain.Resources.ValueObjects;
using Synentra.Domain.Resources;

namespace Synentra.Application.Features.Resources.Contracts;

public sealed record ResourceResolutionResult(
    ResolutionStatus Status, 
    IReadOnlyList<ResourceDescriptor> Descriptors, 
    IReadOnlyList<string>? Errors = null)
{
    public static ResourceResolutionResult Available(IReadOnlyList<ResourceDescriptor> ds) => new(ResolutionStatus.Available, ds);
    public static ResourceResolutionResult Unavailable(string reason) => new(ResolutionStatus.Unavailable, Array.Empty<ResourceDescriptor>(), new[] { reason });
    public static ResourceResolutionResult NotFound(string reason) => new(ResolutionStatus.NotFound, Array.Empty<ResourceDescriptor>(), new[] { reason });
    public static ResourceResolutionResult Unauthorized(string reason) => new(ResolutionStatus.Unauthorized, Array.Empty<ResourceDescriptor>(), new[] { reason });
}
