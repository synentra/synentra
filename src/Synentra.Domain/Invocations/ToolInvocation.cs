using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Invocations.ValueObjects;
using Synentra.Domain.Operations.ValueObjects;
using Synentra.Domain.Resources.ValueObjects;

namespace Synentra.Domain.Invocations;

public sealed class ToolInvocation
{
    public InvocationId Id { get; }
    public int SchemaVersion { get; }
    public string AuthenticatedAgentId { get; }
    public string SecurityScope { get; }
    public OperationId OperationId { get; }
    public string OperationVersion { get; }
    public string OperationFamily { get; }
    public string Action { get; }
    public string BindingId { get; }
    public string BindingVersion { get; }
    public string TargetId { get; }
    public IReadOnlyList<ResourceDescriptor> Resources { get; }
    public IReadOnlyDictionary<string, object> Arguments { get; }
    public IReadOnlyDictionary<string, string> Context { get; }
    public IReadOnlyDictionary<string, string> TransportDetails { get; }
    public BindingDigest Digest { get; }
    public IntentModality Modality { get; }
    public string? DeclaredPurpose { get; }
    public string? TaskGrantId { get; }
    public string? IdempotencyKey { get; }
    public bool RequiresGrant { get; }
    public bool IsMutation { get; }
    public DateTimeOffset ReceiptTime { get; }
    public bool HasEffectRegistration { get; }

    public ToolInvocation(
        InvocationId id, int schemaVersion, 
        string authenticatedAgentId, string securityScope, 
        OperationId operationId, string operationVersion, 
        string operationFamily, string action, 
        string bindingId, string bindingVersion, 
        string targetId, IReadOnlyList<ResourceDescriptor> resources, 
        IReadOnlyDictionary<string, object> arguments, 
        IReadOnlyDictionary<string, string> context, 
        IReadOnlyDictionary<string, string> transportDetails, 
        BindingDigest digest, IntentModality modality, 
        string? declaredPurpose, string? taskGrantId, 
        string? idempotencyKey, bool requiresGrant, 
        bool isMutation, bool hasEffectRegistration, 
        DateTimeOffset receiptTime)
    {
        if (string.IsNullOrWhiteSpace(authenticatedAgentId))
            throw new DomainException(DomainErrors.ToolInvocation.InvalidAuthenticatedAgentId);

        Id = id;
        SchemaVersion = schemaVersion;
        AuthenticatedAgentId = authenticatedAgentId;
        SecurityScope = securityScope;
        OperationId = operationId;
        OperationVersion = operationVersion;
        OperationFamily = operationFamily;
        Action = action;
        BindingId = bindingId;
        BindingVersion = bindingVersion;
        TargetId = targetId;
        Resources = resources;
        Arguments = Freeze(arguments);
        Context = Freeze(context);
        TransportDetails = Freeze(transportDetails);
        Digest = digest;
        Modality = modality;
        DeclaredPurpose = declaredPurpose;
        TaskGrantId = taskGrantId;
        IdempotencyKey = idempotencyKey;
        RequiresGrant = requiresGrant;
        IsMutation = isMutation;
        HasEffectRegistration = hasEffectRegistration;
        ReceiptTime = receiptTime;
    }

    private static IReadOnlyDictionary<K, V> Freeze<K, V>(IReadOnlyDictionary<K, V> src)
        where K : notnull => new System.Collections.ObjectModel.ReadOnlyDictionary<K, V>(new Dictionary<K, V>(src));
}
