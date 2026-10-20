using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Invocations;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Features.Invocations.Normalization;

public interface IInvocationNormalizer
{
    IntentModality Modality { get; }
    Task<ToolInvocation> NormalizeAsync(NormalizationInput input, CanonicalPrincipal principal, CancellationToken ct);
}
