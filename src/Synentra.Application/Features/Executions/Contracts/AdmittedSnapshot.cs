using Synentra.Domain.Invocations.ValueObjects;
using Synentra.Domain.Operations.ValueObjects;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Features.Executions.Contracts;

public sealed record AdmittedSnapshot
{
    public InvocationId InvocationId { get; }
    public OperationId OperationId { get; }
    public string BindingId { get; }
    public string TargetId { get; }
    public string Method { get; }
    public string Path { get; }
    public IReadOnlyDictionary<string, object> Arguments { get; }
    public IReadOnlyDictionary<string, string> Headers { get; }
    public BindingDigest Digest { get; }
    public string? TaskGrantId { get; }
    public string? IdempotencyKey { get; }
    public string? IdempotencyHeader { get; }
    public string? ApprovalId { get; }
    public CredentialBinding? CredentialBinding { get; }
    public bool IsMutation { get; }

    public AdmittedSnapshot(
        InvocationId invocationId, 
        OperationId operationId, 
        string bindingId, 
        string targetId, 
        string method, 
        string path, 
        IReadOnlyDictionary<string, object> arguments, 
        IReadOnlyDictionary<string, string> headers, 
        BindingDigest digest, 
        string? taskGrantId, 
        string? idempotencyKey, 
        string? idempotencyHeader, 
        string? approvalId, 
        CredentialBinding? credentialBinding, 
        bool isMutation)
    {
        InvocationId = invocationId;
        OperationId = operationId;
        BindingId = bindingId;
        TargetId = targetId;
        Method = method;
        Path = path;
        Arguments = arguments;
        Headers = headers;
        Digest = digest;
        TaskGrantId = taskGrantId;
        IdempotencyKey = idempotencyKey;
        IdempotencyHeader = idempotencyHeader;
        ApprovalId = approvalId;
        CredentialBinding = credentialBinding;
        IsMutation = isMutation;
    }
}
