using Synentra.Domain.Governance.Intent;

namespace Synentra.Application.Features.Invocations.Contracts;

public sealed record ToolInvocationData(
    string OperationId, 
    string OperationFamily, 
    string Action, 
    IntentModality Modality, 
    IReadOnlyDictionary<string, object> Arguments, 
    IReadOnlyDictionary<string, string> TransportDetails);
