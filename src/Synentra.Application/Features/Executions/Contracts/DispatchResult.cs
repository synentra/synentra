namespace Synentra.Application.Features.Executions.Contracts;

public sealed record DispatchResult(
    TransportReceipt Receipt, 
    string? ExecutionRecordId);
