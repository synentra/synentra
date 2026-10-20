using System.Text.Json;
using Synentra.Domain.Approvals;
using Synentra.Domain.Grants;
using Synentra.Domain.Grants.ValueObjects;
using Synentra.Domain.Execution;
using Synentra.Domain.Invocations.ValueObjects;

namespace Synentra.Persistence.Serialization;

internal static class AggregateReader
{
    private static T Read<T>(JsonElement e, string key) => e.GetProperty(key).Deserialize<T>(PayloadCodec.Json)!;

    public static Approval Approval(JsonElement e) 
        => Domain.Approvals.Approval.Restore(
            Read<string>(e, "id"), 
            Read<InvocationId>(e, "invocationId"), 
            Read<BindingDigest>(e, "bindingDigest"), 
            Read<string>(e, "requestingAgentId"), 
            Read<string>(e, "operationId"), 
            Read<string>(e, "bindingId"), 
            Read<string?>(e, "taskGrantId"), 
            Read<string>(e, "policyRevision"),
            Read<string>(e, "reviewObligationRevision"), 
            Read<string>(e, "securityScope"), 
            Read<DateTimeOffset>(e, "expiresAt"), 
            Read<DateTimeOffset>(e, "createdAt"), 
            Read<ApprovalStatus>(e, "status"), 
            Read<string?>(e, "reviewerId"),
            Read<DateTimeOffset?>(e, "resolvedAt"));

    public static TaskGrant Grant(JsonElement e) 
        => TaskGrant.Restore(
            Read<string>(e, "id"), 
            Read<string>(e, "issuerId"), 
            Read<string>(e, "agentId"), 
            Read<string>(e, "securityScope"), 
            Read<List<string>>(e, "operationIds"), 
            Read<List<ResourceScope>>(e, "resourceScopes"), 
            Read<List<GrantLimit>>(e, "limits"), 
            Read<DateTimeOffset>(e, "expiresAt"), 
            Read<string?>(e, "externalReference"), 
            Read<GrantState>(e, "state"), 
            Read<DateTimeOffset>(e, "createdAt"));

    public static ExecutionRecord Execution(JsonElement e) 
        => ExecutionRecord.Restore(
            Read<string>(e, "id"), 
            Read<InvocationId>(e, "invocationId"), 
            Read<ExecutionStatus>(e, "status"), 
            Read<string?>(e, "upstreamReference"), 
            Read<DateTimeOffset?>(e, "dispatchedAt"), 
            Read<DateTimeOffset?>(e, "respondedAt"), 
            Read<string?>(e, "errorMessage"));
}
