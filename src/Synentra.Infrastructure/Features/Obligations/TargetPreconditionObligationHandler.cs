using Synentra.Application.Features.Obligations.Contracts;
using Synentra.Domain.Governance.Obligations;

namespace Synentra.Infrastructure.Features.Obligations;

public sealed class TargetPreconditionObligationHandler : IObligationHandler
{
    public string HandlerId => "target-precondition";
    public string Version => "1.0.0";
    public ObligationPhase Phase => ObligationPhase.BeforeDispatch;

    public Task<ObligationResult> HandleAsync(
        ObligationContext context, 
        CancellationToken ct) 
        => Task.FromResult(
            context.Parameters.TryGetValue("expectedVersion", out var expected) && 
            context.Parameters.TryGetValue("currentVersion", out var current) && 
            expected is not null && 
            current is not null && 
            expected.ToString() == current.ToString() ? ObligationResult.Ok() : ObligationResult.Fail("PRECONDITION_NOT_ESTABLISHED"));
}
