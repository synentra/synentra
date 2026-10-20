using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Synentra.Application.Features.Policies.Contracts;
using Synentra.Infrastructure.Configuration;
using Synentra.Infrastructure.Networking;

namespace Synentra.Infrastructure.Features.Policies;

public sealed class OpaPolicyEngine(IHttpClientFactory clients, IOptions<InfrastructureOptions> options) : IPolicyEngine
{
    public string EngineId => "opa";
    public string Version => "1.0.0";

    public async Task<PolicyResult> EvaluateAsync(PolicyInput input, CancellationToken ct)
    {
        if (input.Trust?.Quarantined == true)
            return PolicyResult.Deny(new[] { "AGENT_QUARANTINED" });

        var o = options.Value;
        try
        {
            var path = string.Join('/', o.OpaPolicyPath.Split('/').Select(Uri.EscapeDataString));
            var endpoint = new Uri(new Uri(o.OpaEndpoint ?? throw new InvalidOperationException("OPA endpoint missing.")), "v1/data/" + path);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new { input = new { schema = "synentra.policy-input.v1", agent = new { id = input.AuthenticatedAgentId, securityScope = input.SecurityScope }, operation = input.Invocation, resources = input.Resources, grant = input.TaskGrantId, evidence = new { intent = input.Intent, risk = input.Risk, trust = input.Trust }, untrustedContext = input.UntrustedContext } })
            };
            using var response = await clients.CreateClient("synentra-governed").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await BoundedResponse.ReadAsync(response.Content, o.MaximumResponseBytes, ct));
            if (!doc.RootElement.TryGetProperty("result", out var result))
                return PolicyResult.Deny(new[] { "OPA_UNDEFINED" });

            if (result.ValueKind == JsonValueKind.True)
                return PolicyResult.Allow(new[] { "OPA_ALLOW" }, "opa");

            if (result.ValueKind == JsonValueKind.False)
                return PolicyResult.Deny(new[] { "OPA_DENY" }, "opa");

            if (result.ValueKind != JsonValueKind.Object)
                return PolicyResult.Deny(new[] { "OPA_INVALID_RESULT" });

            bool Flag(string key) => result.TryGetProperty(key, out var x) && x.ValueKind == JsonValueKind.True;
            var revision = result.TryGetProperty("revision", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString()! : "opa";

            var reasons = result.TryGetProperty("reasons", out var reasonsElement) && reasonsElement.ValueKind == JsonValueKind.Array ? reasonsElement.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToArray() : new[]
            {
                "OPA_DECISION"
            };

            return Flag("deny") ? PolicyResult.Deny(reasons, revision) : Flag("review") ? PolicyResult.Review(reasons, revision) : Flag("allow") ? PolicyResult.Allow(reasons, revision) : PolicyResult.Deny(reasons, revision);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)when (ex is HttpRequestException or JsonException or InvalidDataException or TaskCanceledException)
        {
            return PolicyResult.Deny(new[] { "POLICY_BACKEND_UNAVAILABLE" });
        }
    }
}
