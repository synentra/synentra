using Synentra.Domain.Governance.Obligations;

namespace Synentra.Application.Features.Obligations.Contracts;

public interface IObligationHandler
{
    string HandlerId { get; }
    string Version { get; }
    ObligationPhase Phase { get; }
    Task<ObligationResult> HandleAsync(ObligationContext context, CancellationToken ct);
}
