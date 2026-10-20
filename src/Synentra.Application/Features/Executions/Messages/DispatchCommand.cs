using Synentra.Application.Features.Executions.Contracts;

namespace Synentra.Application.Features.Executions.Messages;

public sealed record DispatchCommand(
    string InvocationId, 
    string ClaimId, 
    string ExecutionRecordId, 
    AdmittedSnapshot Snapshot, 
    string ManifestRevision);
