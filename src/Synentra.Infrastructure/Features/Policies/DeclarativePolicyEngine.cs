using System.Text.Json;
using Synentra.Application.Features.Policies.Contracts;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Models;

namespace Synentra.Infrastructure.Features.Policies;

public sealed class DeclarativePolicyEngine(IManifestProvider manifests) : IPolicyEngine
{
    public string EngineId => "declarative";
    public string Version => "1.0.0";

    public Task<PolicyResult> EvaluateAsync(PolicyInput input, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var revision = manifests.ActiveRevision;
        if (input.Trust?.Quarantined == true)
            return Task.FromResult(PolicyResult.Deny(new[] { "AGENT_QUARANTINED" }, revision));

        var bundle = manifests.ActiveBundle;
        var op = bundle?.Spec.Operations.SingleOrDefault(x => x.Id == input.Invocation.OperationId);
        if (op is null)
            return Task.FromResult(PolicyResult.Deny(new[] { "OPERATION_NOT_REGISTERED" }, revision));

        var profile = bundle!.Spec.Profiles.SingleOrDefault(x => x.Id == op.ProfileRef);
        var matches = new List<string>();
        bool allow = false, review = false;
        foreach (var id in op.PolicyRefs)
        {
            var policy = bundle.Spec.Policies.SingleOrDefault(x => x.Id == id);
            if (policy is null)
                return Task.FromResult(PolicyResult.Deny(new[] { "POLICY_NOT_FOUND" }, revision));

            if (policy.OperationFamily is not null && policy.OperationFamily != input.Invocation.OperationFamily)
                continue;

            for (int i = 0; i < policy.Rules.Count; i++)
            {
                var rule = policy.Rules[i];
                if (!Matches(rule.Match, input))
                    continue;
                matches.Add($"{id}:{i}");
                switch (rule.Effect)
                {
                    case "deny":
                        return Task.FromResult(PolicyResult.Deny(new[] { "EXPLICIT_DENY" }, revision, matches));
                    case "review":
                        review = true;
                        break;
                    case "allow":
                        allow = true;
                        break;
                    default:
                        return Task.FromResult(PolicyResult.Deny(new[] { "UNKNOWN_POLICY_EFFECT" }, revision));
                }
            }
        }

        return Task.FromResult(review ? PolicyResult.Review(new[] { "REVIEW_REQUIRED" }, revision, matches) : allow ? PolicyResult.Allow(new[] { "POLICY_ALLOW" }, revision, matches) : profile?.Policy?.DefaultOutcome == "review" ? PolicyResult.Review(new[] { "DEFAULT_REVIEW" }, revision) : profile?.Policy?.DefaultOutcome == "allow" ? PolicyResult.Allow(new[] { "DEFAULT_ALLOW" }, revision) : PolicyResult.Deny(new[] { "NO_APPLICABLE_PERMISSION" }, revision));
    }

    private static bool Matches(PolicyMatchModel m, PolicyInput input)
    {
        if (m.AgentIds is { Count: > 0 } && !m.AgentIds.Contains(input.AuthenticatedAgentId))
            return false;

        if (m.OperationIds is { Count: > 0 } && !m.OperationIds.Contains(input.Invocation.OperationId))
            return false;

        if (m.OperationFamilies is { Count: > 0 } && !m.OperationFamilies.Contains(input.Invocation.OperationFamily))
            return false;

        foreach (var match in m.ResourceMatches ?? new())
        {
            var candidates = input.Resources.Where(r => r.Type == match.Type && r.SecurityScope == input.SecurityScope && r.TrustedAttributes.TryGetValue("role", out var role) && role == match.Role).ToList();
            if (candidates.Count == 0)
                return false;
            bool Match(Domain.Resources.ValueObjects.ResourceDescriptor r) => match.Ids is not { Count: > 0 } || match.Ids.Contains(r.Id);
            if ((match.Quantifier ?? "all") switch
            {
                "all" => !candidates.All(Match),
                "any" => !candidates.Any(Match),
                _ => true
            })
                return false;
        }

        var arguments = JsonSerializer.SerializeToElement(input.Invocation.Arguments);
        foreach (var(pointer, expected)in m.ArgumentEquals ?? new())
            if (!JsonPointer.TryRead(arguments, pointer, out var actual) || !JsonPointer.Equal(actual, expected))
                return false;

        foreach (var(pointer, threshold)in m.ArgumentIntegerGreaterThan ?? new())
            if (!JsonPointer.TryRead(arguments, pointer, out var actual) || actual.ValueKind != JsonValueKind.Number || !actual.TryGetInt64(out var n) || n <= threshold)
                return false;

        return true;
    }
}
