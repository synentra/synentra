using Synentra.Domain.Effects.ValueObjects;
using Synentra.Domain.Invocations;
using Synentra.Domain.Resources.ValueObjects;

namespace Synentra.Application.Features.Effects.Contracts;

public interface IEffectResolver
{
    string ResolverId { get; }
    string Version { get; }
    Task<EffectPlan> ResolveAsync(
        ToolInvocation invocation, 
        IReadOnlyList<ResourceDescriptor> resources, 
        EffectRegistration registration, 
        CancellationToken ct);
}
