using Synentra.Domain.Effects.ValueObjects;
using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Governance.Evidence;
using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Policies;
using Synentra.Domain.Invocations;
using Synentra.Domain.Resources.ValueObjects;
using Synentra.Domain.Resources;
using Microsoft.Extensions.Logging;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Effects.Contracts;
using Synentra.Application.Features.Intent.Contracts;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Models;
using Synentra.Application.Features.Obligations.Contracts;
using Synentra.Application.Features.Policies.Contracts;
using Synentra.Application.Features.Resources.Contracts;
using Synentra.Application.Features.Risk.Contracts;
using Synentra.Application.Features.Trust.Contracts;

namespace Synentra.Application.Features.Invocations.Evaluation;

public sealed class DecisionEngine : IInvocationEvaluator
{
    private readonly IReadOnlyList<IIntentProvider> _intentProviders;
    private readonly IRiskEngine _riskEngine;
    private readonly ITrustEngine _trustEngine;
    private readonly IPolicyEngine _policyEngine;
    private readonly IEnumerable<IResourceResolver> _resourceResolvers;
    private readonly IEnumerable<IEffectResolver> _effectResolvers;
    private readonly IEnumerable<IObligationHandler> _obligationHandlers;
    private readonly IManifestProvider _manifests;
    private readonly IRevisionProvider _revisions;
    private readonly IAuditStore _audit;
    private readonly ILogger<DecisionEngine> _log;

    public DecisionEngine(
        IEnumerable<IIntentProvider> intentProviders, 
        IRiskEngine riskEngine, 
        ITrustEngine trustEngine, 
        IPolicyEngine policyEngine, 
        IEnumerable<IResourceResolver> resourceResolvers, 
        IEnumerable<IEffectResolver> effectResolvers, 
        IEnumerable<IObligationHandler> obligationHandlers, 
        IManifestProvider manifests, 
        IRevisionProvider revisions, 
        IAuditStore audit, 
        ILogger<DecisionEngine> log)
    {
        _intentProviders = intentProviders.ToList();
        _riskEngine = riskEngine;
        _trustEngine = trustEngine;
        _policyEngine = policyEngine;
        _resourceResolvers = resourceResolvers;
        _effectResolvers = effectResolvers;
        _obligationHandlers = obligationHandlers;
        _manifests = manifests;
        _revisions = revisions;
        _audit = audit;
        _log = log;
    }

    public async Task<ExecutionDecision> EvaluateAsync(ToolInvocation invocation, CancellationToken ct)
    {
        await _revisions.ReloadAsync(ct);
        var bundle = _manifests.ActiveBundle;
        if (bundle is null)
            return ExecutionDecision.Deny(new[] { "NO_ACTIVE_BUNDLE" });

        var trust = await _trustEngine.LoadSnapshotAsync(invocation.AuthenticatedAgentId, invocation.SecurityScope, ct);
        if (trust.Quarantined)
            return ExecutionDecision.Deny(new[] { "AGENT_QUARANTINED" });

        // 1. Hard restrictions first — no model/trust/approval override
        var hardDeny = await EvaluateHardRestrictionsAsync(invocation, bundle, ct);
        if (hardDeny is not null)
            return hardDeny;

        // 2. Intent classification (may be disabled by profile)
        var profile = ResolveProfile(invocation, bundle);
        var intent = await EvaluateIntentAsync(invocation, profile, ct);

        // If the profile requires intent and it's missing → deny/review per config
        if (profile?.Intent?.Required == true && (intent is null 
            || intent.Status is IntentStatus.Unsupported or IntentStatus.LowConfidence or IntentStatus.Timeout or IntentStatus.Error))
        {
            var onUnavailable = profile?.Intent?.OnUnavailable;
            if (onUnavailable == "deny")
                return ExecutionDecision.Deny(new[] { "INTENT_UNAVAILABLE" });

            if (onUnavailable == "review")
                return ExecutionDecision.Review(new[] { "INTENT_UNAVAILABLE" });
        }

        // 3. Trust snapshot
        // 4. Risk scoring
        var riskInput = new RiskInput(
            new ToolInvocationData(
                invocation.OperationId.Value, 
                invocation.OperationFamily, 
                invocation.Action, 
                invocation.Modality, 
                invocation.Arguments, 
                invocation.TransportDetails), 
            intent, trust);

        var risk = await _riskEngine.EvaluateAsync(riskInput, ct);

        // 5. Resource resolution
        var resourceResults = new List<ResourceDescriptor>();
        foreach (var registration in ResolveResourceRegistrations(invocation, bundle))
        {
            var resolver = _resourceResolvers.FirstOrDefault(r => r.ResolverId == registration.ResolverRef && r.Version == registration.Version);
            if (resolver is null)
                return ExecutionDecision.Deny(new[] { $"RESOLVER_NOT_FOUND:{registration.ResolverRef}" });

            var result = await resolver.ResolveAsync(invocation, registration, ct);
            if (result.Status == ResolutionStatus.Unavailable)
                return ExecutionDecision.Deny(new[] { "RESOURCE_UNAVAILABLE" });

            if (result.Status == ResolutionStatus.Unauthorized)
                return ExecutionDecision.Deny(new[] { "RESOURCE_UNAUTHORIZED" });

            if (result.Status == ResolutionStatus.NotFound)
                return ExecutionDecision.Deny(new[] { "RESOURCE_NOT_FOUND" });

            resourceResults.AddRange(result.Descriptors);
        }

        // 6. Effect resolution (optional)
        EffectPlan? effectPlan = null;
        var effectReg = ResolveEffectRegistration(invocation, bundle);
        if (effectReg is not null)
        {
            var eff = _effectResolvers.FirstOrDefault(r => r.ResolverId == effectReg.ResolverRef && r.Version == effectReg.Version);
            if (eff is null)
                return ExecutionDecision.Deny(new[] { "EFFECT_RESOLVER_NOT_FOUND" });
            if (eff is not null)
            {
                effectPlan = await eff.ResolveAsync(invocation, resourceResults, effectReg, ct);
                if (effectPlan.HasUnknownUpperBound)
                    return ExecutionDecision.Deny(new[] { "UNKNOWN_EFFECT_BOUND" });
            }
        }

        // 7. Policy evaluation with complete evidence
        var policyInput = new PolicyInput(
            new ToolInvocationData(
                invocation.OperationId.Value, 
                invocation.OperationFamily, 
                invocation.Action, 
                invocation.Modality, 
                invocation.Arguments, 
                invocation.TransportDetails),
            invocation.AuthenticatedAgentId, 
            invocation.SecurityScope, 
            intent, 
            risk, 
            trust, 
            resourceResults, 
            effectPlan, 
            invocation.TaskGrantId, 
            invocation.Context);

        var policyResult = await _policyEngine.EvaluateAsync(policyInput, ct);

        // 8. Precedence: Deny > Review > Allow
        var evidence = new GovernanceEvidence
        {
            ManifestRevision = _revisions.ActiveRevision,
            Intent = intent,
            Risk = risk,
            Trust = trust,
            Resources = resourceResults,
            Effect = effectPlan
        };

        await _audit.AppendAsync(
            new AuditEntry(
                Guid.NewGuid().ToString("N"), 
                "decision",
                invocation.AuthenticatedAgentId,
                invocation.SecurityScope, 
                invocation.OperationId.Value, 
                invocation.BindingId, 
                invocation.TargetId,
                invocation.Id.ToString(),
                invocation.Digest.Value,
                policyResult.PolicyRevision, 
                null, 
                new Dictionary<string, string> { ["outcome"] = policyResult.Outcome.ToString(), ["reasons"] = string.Join(",", policyResult.ReasonCodes) }, 
                DateTimeOffset.UtcNow), 
            ct);

        return policyResult.Outcome switch
        {
            PolicyOutcome.Deny => ExecutionDecision.Deny(policyResult.ReasonCodes, evidence, policyResult.PolicyRevision),
            PolicyOutcome.Review => ExecutionDecision.Review(policyResult.ReasonCodes, evidence, policyResult.PolicyRevision),
            PolicyOutcome.Allow => ExecutionDecision.Allow(policyResult.ReasonCodes, evidence, policyResult.PolicyRevision),
            _ => ExecutionDecision.Deny(new[] { "UNKNOWN_POLICY_OUTCOME" })};
    }

    private async Task<ExecutionDecision?> EvaluateHardRestrictionsAsync(ToolInvocation invocation, GovernanceBundleModel bundle, CancellationToken ct)
    {
        // Global restrictions apply to all bindings and cannot be overridden
        foreach (var restriction in bundle.Spec.Restrictions)
        {
            if (restriction.Match.AgentIds is { Count: > 0 } agentIds && !agentIds.Contains(invocation.AuthenticatedAgentId))
                continue;

            if (restriction.Match.OperationFamilies is { Count: > 0 } families && !families.Contains(invocation.OperationFamily))
                continue;

            if (restriction.Match.OperationIds is { Count: > 0 } ops && !ops.Contains(invocation.OperationId.Value))
                continue;

            if (restriction.Effect == "deny")
            {
                await _audit.AppendAsync(
                    new AuditEntry(
                        Guid.NewGuid().ToString("N"), 
                        "hard-restriction", 
                        invocation.AuthenticatedAgentId, 
                        invocation.SecurityScope, 
                        invocation.OperationId.Value, 
                        invocation.BindingId, 
                        invocation.TargetId, 
                        invocation.Id.ToString(),
                        invocation.Digest.Value, 
                        bundle.Metadata.Version, 
                        null,
                        new Dictionary<string, string> { ["restrictionId"] = restriction.Id },
                        DateTimeOffset.UtcNow), 
                    ct);

                return ExecutionDecision.Deny(new[] { $"HARD_DENY:{restriction.Id}" }, policyRevision: bundle.Metadata.Version);
            }
        }

        return null;
    }

    private async Task<IntentLabel?> EvaluateIntentAsync(ToolInvocation invocation, ProfileModel? profile, CancellationToken ct)
    {
        if (profile?.Intent is null)
            return null;

        var provider = _intentProviders.
            FirstOrDefault(p => p.ProviderId == profile?.Intent?.ProviderRef) ?? _intentProviders
            .FirstOrDefault(p => p.SupportedModalities.HasFlag(invocation.Modality));

        if (provider is null)
            return null;

        // Unsupported modality must not receive fabricated confidence
        if (!provider.SupportedModalities.HasFlag(invocation.Modality))
            return new IntentLabel("unsupported", 0, IntentStatus.Unsupported, provider.ProviderId);

        try
        {
            return await provider.ClassifyAsync(IntentEvidence.From(invocation), ct);
        }
        catch (OperationCanceledException)
        {
            return new IntentLabel("timeout", 0, IntentStatus.Timeout, provider.ProviderId);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Intent classification failed");
            return new IntentLabel("error", 0, IntentStatus.Error, provider.ProviderId);
        }
    }

    private static ProfileModel? ResolveProfile(ToolInvocation invocation, GovernanceBundleModel bundle)
    {
        var op = bundle.Spec.Operations.FirstOrDefault(o => o.Id == invocation.OperationId.Value);
        if (op?.ProfileRef is null)
            return null;

        return bundle.Spec.Profiles.FirstOrDefault(p => p.Id == op.ProfileRef);
    }

    private static IEnumerable<ResolverRegistration> ResolveResourceRegistrations(ToolInvocation invocation, GovernanceBundleModel bundle)
    {
        var op = bundle.Spec.Operations.FirstOrDefault(o => o.Id == invocation.OperationId.Value);
        if (op is null)
            yield break;

        foreach (var r in op.Resources)
            yield return new ResolverRegistration(r.Role, r.ResolverRef, r.Version, r.Configuration.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? ""));
    }

    private static EffectRegistration? ResolveEffectRegistration(ToolInvocation invocation, GovernanceBundleModel bundle)
    {
        var op = bundle.Spec.Operations.FirstOrDefault(o => o.Id == invocation.OperationId.Value);
        if (op?.Effect is null)
            return null;

        return new EffectRegistration(op.Effect.ResolverRef, op.Effect.Version, op.Effect.Configuration.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? ""));
    }
}
