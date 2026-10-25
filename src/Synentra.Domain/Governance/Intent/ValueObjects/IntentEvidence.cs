using Synentra.Domain.Invocations;

namespace Synentra.Domain.Governance.Intent.ValueObjects;

public sealed record IntentEvidence
{
    public string OperationId { get; }
    public string? AuthenticatedAgentId { get; }
    public string? SecurityScope { get; }
    public string OperationFamily { get; }
    public string Action { get; }
    public IReadOnlyDictionary<string, object> Arguments { get; }
    public IReadOnlyDictionary<string, string> Context { get; }
    public IntentModality Modality { get; }
    public string? DeclaredPurpose { get; }

    public IntentEvidence(
        string operationId, 
        string operationFamily, 
        string action,
        IReadOnlyDictionary<string, object> arguments,
        IReadOnlyDictionary<string, string> context,
        IntentModality modality, string? declaredPurpose,
        string? authenticatedAgentId = null, 
        string? securityScope = null)
    {
        OperationId = operationId;
        AuthenticatedAgentId = authenticatedAgentId;
        SecurityScope = securityScope;
        OperationFamily = operationFamily;
        Action = action;
        Arguments = arguments;
        Context = context;
        Modality = modality;
        DeclaredPurpose = declaredPurpose;
    }

    public static IntentEvidence From(ToolInvocation invocation) 
        => new(invocation.OperationId.Value, invocation.OperationFamily, 
            invocation.Action, invocation.Arguments, invocation.Context, 
            invocation.Modality, invocation.DeclaredPurpose, 
            invocation.AuthenticatedAgentId, invocation.SecurityScope);
}
