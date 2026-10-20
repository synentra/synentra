using Synentra.Application.Features.Executions.Contracts;
using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Invocations.ValueObjects;

namespace Synentra.Application.Features.Obligations.Contracts;

public sealed record ObligationContext(
    InvocationId InvocationId, 
    AdmittedSnapshot? Snapshot, 
    ExecutionDecision Decision, 
    IReadOnlyDictionary<string, object> Parameters);
