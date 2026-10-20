using Synentra.Application.Common.Plugins;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Models;

namespace Synentra.Application.Features.Manifests;

public sealed class BundleValidator
{
    private static readonly HashSet<string> AllowedApiVersions = new()
    {
        "synentra.io/v1alpha2"
    };
    private static readonly HashSet<string> AllowedExecutionModels = new()
    {
        "request_response",
        "asynchronous",
        "stream",
        "batch"
    };
    private static readonly HashSet<string> AllowedMutability = new()
    {
        "read_only",
        "mutation",
        "mixed"
    };
    private static readonly HashSet<string> AllowedEffects = new()
    {
        "allow",
        "review",
        "deny"
    };
    private static readonly HashSet<string> AllowedQuantifiers = new()
    {
        "all",
        "any"
    };
    private readonly IPluginResolver _plugins;
    public BundleValidator(IPluginResolver plugins) => _plugins = plugins;

    public ValidationResult Validate(GovernanceBundleModel bundle)
    {
        var issues = new List<ValidationIssue>();
        // 1. API version and kind
        if (!AllowedApiVersions.Contains(bundle.ApiVersion))
            issues.Add(new("error", "UNSUPPORTED_API_VERSION", $"apiVersion must be one of: {string.Join(", ", AllowedApiVersions)}. Found: {bundle.ApiVersion}."));

        if (bundle.Kind != "GovernanceBundle")
            issues.Add(new("error", "INVALID_KIND", $"kind must be GovernanceBundle. Found: {bundle.Kind}."));

        // 2. Unique IDs within each kind
        ValidateUniqueIds(issues, bundle.Spec.Profiles.Select(p => p.Id), "profile");
        ValidateUniqueIds(issues, bundle.Spec.Targets.Select(t => t.Id), "target");
        ValidateUniqueIds(issues, bundle.Spec.Operations.Select(o => o.Id), "operation");
        ValidateUniqueIds(issues, bundle.Spec.Bindings.Select(b => b.Id), "binding");
        ValidateUniqueIds(issues, bundle.Spec.Restrictions.Select(r => r.Id), "restriction");
        ValidateUniqueIds(issues, bundle.Spec.Policies.Select(p => p.Id), "policy");

        // 3. Reference resolution
        var profileIds = bundle.Spec.Profiles.Select(p => p.Id).ToHashSet();
        var targetIds = bundle.Spec.Targets.Select(t => t.Id).ToHashSet();
        var opIds = bundle.Spec.Operations.Select(o => o.Id).ToHashSet();
        var policyIds = bundle.Spec.Policies.Select(p => p.Id).ToHashSet();

        foreach (var op in bundle.Spec.Operations)
        {
            if (op.ProfileRef is not null && !profileIds.Contains(op.ProfileRef))
                issues.Add(new("error", "UNRESOLVED_PROFILE_REF", $"Operation '{op.Id}' references unknown profile '{op.ProfileRef}'."));

            foreach (var pr in op.PolicyRefs)
                if (!policyIds.Contains(pr))
                    issues.Add(new("error", "UNRESOLVED_POLICY_REF", $"Operation '{op.Id}' references unknown policy '{pr}'."));

            if (!AllowedMutability.Contains(op.Mutability))
                issues.Add(new("error", "INVALID_MUTABILITY", $"Operation '{op.Id}' has invalid mutability '{op.Mutability}'."));

            if (!AllowedExecutionModels.Contains(op.Execution.Model))
                issues.Add(new("error", "INVALID_EXECUTION_MODEL", $"Operation '{op.Id}' has invalid execution model '{op.Execution.Model}'."));

            if (op.Effect is not null && !_plugins.HasRegistration(op.Effect.ResolverRef))
                issues.Add(new("error", "UNRESOLVED_EFFECT_RESOLVER", $"Operation '{op.Id}' references unregistered effect resolver."));

            foreach (var r in op.Resources)
                if (!_plugins.HasRegistration(r.ResolverRef))
                    issues.Add(new("error", "UNRESOLVED_RESOURCE_RESOLVER", $"Operation '{op.Id}' references unregistered resource resolver '{r.ResolverRef}'."));

            if (op.Verification is not null && !_plugins.HasRegistration(op.Verification.VerifierRef))
                issues.Add(new("error", "UNRESOLVED_VERIFIER", $"Operation '{op.Id}' references unregistered verifier '{op.Verification.VerifierRef}'."));

            // Resource role uniqueness
            var roles = op.Resources.Select(r => r.Role).ToList();
            if (roles.Count != roles.Distinct().Count())
                issues.Add(new("error", "DUPLICATE_RESOURCE_ROLE", $"Operation '{op.Id}' has duplicate resource roles."));
        }

        foreach (var b in bundle.Spec.Bindings)
        {
            if (!opIds.Contains(b.OperationId))
                issues.Add(new("error", "UNRESOLVED_OPERATION_REF", $"Binding '{b.Id}' references unknown operation '{b.OperationId}'."));

            if (!targetIds.Contains(b.TargetRef))
                issues.Add(new("error", "UNRESOLVED_TARGET_REF", $"Binding '{b.Id}' references unknown target '{b.TargetRef}'."));

            // Mapping: exactly one source per destination, no overlaps
            ValidateMapping(issues, b.Id, b.CanonicalToTarget, "canonicalToTarget");
            ValidateMapping(issues, b.Id, b.TargetToCanonical, "targetToCanonical");
        }

        foreach (var t in bundle.Spec.Targets)
        {
            if (!_plugins.HasRegistration(t.AdapterRef))
                issues.Add(new("error", "UNRESOLVED_ADAPTER_REF", $"Target '{t.Id}' references unregistered adapter '{t.AdapterRef}'."));
        }

        // 4. Policy rules validate effects and matcher language
        foreach (var policy in bundle.Spec.Policies)
        {
            foreach (var rule in policy.Rules)
            {
                if (!AllowedEffects.Contains(rule.Effect))
                    issues.Add(new("error", "INVALID_POLICY_EFFECT", $"Policy '{policy.Id}' has invalid effect '{rule.Effect}'."));

                foreach (var rm in rule.Match.ResourceMatches ?? new())
                    if (rm.Quantifier is not null && !AllowedQuantifiers.Contains(rm.Quantifier))
                        issues.Add(new("error", "INVALID_QUANTIFIER", $"Policy '{policy.Id}' has invalid quantifier '{rm.Quantifier}'."));
            }
        }

        return new ValidationResult(issues.All(i => i.Severity != "error"), issues);
    }

    private static void ValidateUniqueIds(List<ValidationIssue> issues, IEnumerable<string> ids, string kind)
    {
        var list = ids.ToList();
        var dupes = list.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var d in dupes)
            issues.Add(new("error", $"DUPLICATE_{kind.ToUpperInvariant()}_ID", $"Duplicate {kind} id '{d}'."));

        foreach (var id in list.Where(string.IsNullOrWhiteSpace))
            issues.Add(new("error", $"EMPTY_{kind.ToUpperInvariant()}_ID", $"{kind} has empty id."));
    }

    private static void ValidateMapping(List<ValidationIssue> issues, string bindingId, Dictionary<string, MappingEntryModel>? mapping, string side)
    {
        if (mapping is null)
            return;

        foreach (var(dest, entry)in mapping)
        {
            var sourceCount = 0;
            if (entry.InputPointer is not null)
                sourceCount++;

            if (entry.TargetPointer is not null)
                sourceCount++;

            if (entry.ResourceField is not null)
                sourceCount++;

            if (entry.Constant is not null)
                sourceCount++;

            if (sourceCount == 0)
                issues.Add(new("error", "MAPPING_MISSING_SOURCE", $"Binding '{bindingId}' {side} destination '{dest}' has no source."));

            if (sourceCount > 1)
                issues.Add(new("error", "MAPPING_MULTIPLE_SOURCES", $"Binding '{bindingId}' {side} destination '{dest}' has multiple sources."));

            if (!dest.StartsWith('/'))
                issues.Add(new("error", "MAPPING_INVALID_POINTER", $"Binding '{bindingId}' {side} destination '{dest}' is not a JSON Pointer."));
        }
    }
}
