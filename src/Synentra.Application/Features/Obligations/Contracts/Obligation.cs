using Synentra.Domain.Governance.Obligations;

namespace Synentra.Application.Features.Obligations.Contracts;

public sealed record Obligation(
    string Id, 
    string Version, 
    IReadOnlyDictionary<string, string> Parameters, 
    ObligationPhase Phase);
