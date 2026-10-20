using Synentra.Domain.Invocations;

namespace Synentra.Application.Features.Resources.Contracts;

public interface IResourceResolver
{
    string ResolverId { get; }
    string Version { get; }
    Task<ResourceResolutionResult> ResolveAsync(
        ToolInvocation invocation, 
        ResolverRegistration registration, 
        CancellationToken ct);
}
