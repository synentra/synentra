using System.Text.Json;
using NSubstitute;
using Synentra.Application.Common.Plugins;
using Synentra.Application.Common.Versioning;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Manifests.Models;
using Synentra.Domain.Approvals;
using Synentra.Domain.Execution;
using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Governance.Evidence;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Grants;
using Synentra.Domain.Grants.ValueObjects;
using Synentra.Domain.Invocations;
using Synentra.Domain.Invocations.ValueObjects;
using Synentra.Domain.Operations.ValueObjects;
using Synentra.Domain.Resources.ValueObjects;
using Synentra.Domain.Security;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Tests.TestHelpers;

public static class TestDataBuilder
{
    public static CanonicalPrincipal CreatePrincipal(
        string agentId = "agent-1",
        string securityScope = "scope-default",
        PrincipalType type = PrincipalType.Agent)
        => new(agentId, securityScope, "issuer", agentId, new HashSet<string>(), new Dictionary<string, string>(), type);

    public static ToolInvocation CreateInvocation(
        string? id = null,
        string authenticatedAgentId = "agent-1",
        string securityScope = "scope-default",
        string operationId = "orders.create",
        string operationVersion = "1",
        string operationFamily = "orders",
        string action = "create",
        string bindingId = "orders-http",
        string bindingVersion = "1",
        string targetId = "target-orders",
        IReadOnlyList<ResourceDescriptor>? resources = null,
        IReadOnlyDictionary<string, object>? arguments = null,
        IReadOnlyDictionary<string, string>? context = null,
        IReadOnlyDictionary<string, string>? transportDetails = null,
        string digest = "ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890",
        IntentModality modality = IntentModality.Http,
        string? declaredPurpose = "Create customer order",
        string? taskGrantId = null,
        string? idempotencyKey = "idem-1",
        bool requiresGrant = false,
        bool isMutation = true,
        DateTimeOffset? receiptTime = null)
    {
        return new ToolInvocation(
            new InvocationId(Guid.TryParse(id, out var parsedId) ? parsedId : Guid.NewGuid()),
            2,
            authenticatedAgentId,
            securityScope,
            new OperationId(operationId),
            operationVersion,
            operationFamily,
            action,
            bindingId,
            bindingVersion,
            targetId,
            resources ?? Array.Empty<ResourceDescriptor>(),
            arguments ?? new Dictionary<string, object> { ["amount"] = 100 },
            context ?? new Dictionary<string, string>(),
            transportDetails ?? new Dictionary<string, string>(),
            new BindingDigest(digest),
            modality,
            declaredPurpose,
            taskGrantId,
            idempotencyKey,
            requiresGrant,
            isMutation,
            receiptTime ?? DateTimeOffset.UtcNow);
    }

    public static Approval CreateApproval(
        string id = "approval-1",
        ToolInvocation? invocation = null,
        string policyRevision = "rev-1",
        string reviewObligationRevision = "obl-1",
        DateTimeOffset? expiresAt = null)
    {
        var inv = invocation ?? CreateInvocation();
        return new Approval(
            id,
            inv.Id,
            inv.Digest,
            inv.AuthenticatedAgentId,
            inv.OperationId.Value,
            inv.BindingId,
            inv.TaskGrantId,
            policyRevision,
            reviewObligationRevision,
            inv.SecurityScope,
            expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(15));
    }

    public static TaskGrant CreateTaskGrant(
        string id = "grant-1",
        string issuerId = "issuer-1",
        string agentId = "agent-1",
        string securityScope = "scope-default",
        IReadOnlyList<string>? operationIds = null,
        IReadOnlyList<ResourceScope>? resourceScopes = null,
        DateTimeOffset? expiresAt = null,
        string? externalReference = "ext-ref-1")
    {
        return new TaskGrant(
            id,
            issuerId,
            agentId,
            securityScope,
            operationIds ?? new[] { "orders.create" },
            resourceScopes ?? Array.Empty<ResourceScope>(),
            expiresAt ?? DateTimeOffset.UtcNow.AddHours(1),
            externalReference);
    }

    public static ExecutionRecord CreateExecutionRecord(
        string id = "exec-1",
        string? invocationId = null)
        => new(id, new InvocationId(Guid.TryParse(invocationId, out var parsedId) ? parsedId : Guid.NewGuid()));

    public static ExecutionDecision CreateAllowDecision(
        string manifestRevision = "rev-1",
        string policyRevision = "policy-v1",
        DateTimeOffset? expiresAt = null,
        IReadOnlyList<ResourceDescriptor>? resources = null)
    {
        var evidence = new GovernanceEvidence
        {
            ManifestRevision = manifestRevision,
            Resources = resources ?? Array.Empty<ResourceDescriptor>()
        };
        return ExecutionDecision.Allow(
            new[] { "POLICY_ALLOW" },
            evidence,
            policyRevision);
    }

    public static InvocationRequest CreateInvocationRequest(
        string operationId = "orders.create",
        string? bindingId = "orders-http",
        string? taskGrantId = null,
        object? arguments = null,
        InvocationContext? context = null,
        string schemaVersion = "synentra.invocation.v2")
    {
        var json = JsonSerializer.Serialize(arguments ?? new { amount = 100 });
        using var doc = JsonDocument.Parse(json);
        return new InvocationRequest
        {
            SchemaVersion = schemaVersion,
            OperationId = operationId,
            BindingId = bindingId,
            TaskGrantId = taskGrantId,
            Arguments = doc.RootElement.Clone(),
            Context = context
        };
    }

    public static GovernanceBundleModel CreateValidBundle(
        string name = "orders-bundle",
        string version = "1.0.0")
    {
        return new GovernanceBundleModel
        {
            ApiVersion = CompatibilityPolicy.CurrentManifestApiVersion,
            Kind = "GovernanceBundle",
            Metadata = new BundleMetadata { Name = name, Version = version },
            Spec = new BundleSpec
            {
                Profiles =
                [
                    new ProfileModel
                    {
                        Id = "default-profile",
                        Intent = new IntentProfileModel
                        {
                            ProviderRef = "intent-default",
                            Required = false,
                            MinimumConfidence = 0.8,
                            OnUnavailable = "review"
                        }
                    }
                ],
                Targets =
                [
                    new TargetModel
                    {
                        Id = "target-orders",
                        AdapterRef = "http-adapter"
                    }
                ],
                Operations =
                [
                    new OperationModel
                    {
                        Id = "orders.create",
                        Version = "1",
                        Family = "orders",
                        Action = "create",
                        Mutability = "mutation",
                        Execution = new ExecutionMode { Mode = "requestResponse" },
                        ProfileRef = "default-profile",
                        PolicyRefs = ["policy-orders"],
                        Resources =
                        [
                            new ResolverModel
                            {
                                Role = "order",
                                ResolverRef = "order-resolver",
                                Version = "1"
                            }
                        ],
                        Verification = new VerificationModel
                        {
                            VerifierRef = "order-verifier",
                            Version = "1"
                        }
                    }
                ],
                OperationBindings =
                [
                    new OperationBindingModel
                    {
                        Id = "orders-http",
                        OperationRef = "orders.create",
                        TargetRef = "target-orders",
                        CanonicalToTarget = new Dictionary<string, MappingEntryModel>
                        {
                            ["/body/amount"] = new() { SourcePointer = "/amount" }
                        },
                        TargetToCanonical = new Dictionary<string, MappingEntryModel>
                        {
                            ["/result/id"] = new() { TargetPointer = "/id" }
                        }
                    }
                ],
                Policies =
                [
                    new PolicyModel
                    {
                        Id = "policy-orders",
                        Rules =
                        [
                            new PolicyRuleModel
                            {
                                Id = "allow-rule",
                                Effect = "allow",
                                Match = new PolicyMatchModel()
                            }
                        ]
                    }
                ],
                Restrictions = []
            }
        };
    }

    public static IPluginRegistration CreatePluginRegistration(
        string id = "http-adapter",
        string version = "1.0.0",
        PluginFamily family = PluginFamily.TransportAdapter,
        string contractVersion = "v2",
        params string[] capabilities)
    {
        var reg = Substitute.For<IPluginRegistration>();
        reg.Id.Returns(id);
        reg.Version.Returns(version);
        reg.Family.Returns(family);
        reg.ContractVersion.Returns(contractVersion);
        reg.Capabilities.Returns(new HashSet<string>(capabilities));
        reg.ConfigurationJsonSchema.Returns("{}");
        reg.SupportedInputShapes.Returns(Array.Empty<Type>());
        reg.SupportedOutputShapes.Returns(Array.Empty<Type>());
        reg.SecurityAssumptions.Returns(new SecurityAssumptions(true, false, "none", Array.Empty<string>()));
        reg.ConformanceTests.Returns(Array.Empty<ConformanceTest>());
        return reg;
    }

    public static AdmittedSnapshot CreateAdmittedSnapshot(
        ToolInvocation? invocation = null,
        AuthorizedHttpTarget? target = null)
    {
        var inv = invocation ?? CreateInvocation();
        var authTarget = target ?? new AuthorizedHttpTarget(
            inv.TargetId,
            new Uri("https://api.example.com"),
            new Uri("https://api.example.com/v1/orders"),
            "rev-1");

        return new AdmittedSnapshot(
            inv.Id,
            inv.OperationId,
            inv.BindingId,
            inv.TargetId,
            authTarget,
            true,
            "POST",
            "/v1/orders",
            inv.Arguments,
            new Dictionary<string, string>(),
            inv.Digest,
            inv.TaskGrantId,
            inv.IdempotencyKey,
            "Idempotency-Key",
            null,
            null,
            inv.IsMutation);
    }
}
