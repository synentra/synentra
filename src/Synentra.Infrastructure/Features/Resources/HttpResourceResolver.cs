using System.Net.Http.Json;
using System.Text.Json;
using Synentra.Application.Features.Resources.Contracts;
using Synentra.Domain.Invocations;
using Synentra.Domain.Resources.ValueObjects;
using Synentra.Infrastructure.Networking;

namespace Synentra.Infrastructure.Features.Resources;

public sealed class HttpResourceResolver(IHttpClientFactory clients) : IResourceResolver
{
    public string ResolverId => "http-resource";
    public string Version => "1.0.0";

    public async Task<ResourceResolutionResult> ResolveAsync(ToolInvocation invocation, ResolverRegistration registration, CancellationToken ct)
    {
        if (!registration.Configuration.TryGetValue("endpoint", out var endpoint))
            return ResourceResolutionResult.Unavailable("RESOURCE_ENDPOINT_MISSING");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new { operationId = invocation.OperationId.Value, arguments = invocation.Arguments, scope = invocation.SecurityScope, role = registration.Role })
        };
        using var response = await clients.CreateClient("synentra-governed").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return ResourceResolutionResult.NotFound("RESOURCE_NOT_FOUND");

        if (response.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized)
            return ResourceResolutionResult.Unauthorized("RESOURCE_ACCESS_DENIED");

        if (!response.IsSuccessStatusCode)
            return ResourceResolutionResult.Unavailable("RESOURCE_BACKEND_UNAVAILABLE");

        var rows = JsonSerializer.Deserialize<List<ResourceResponse>>(await BoundedResponse.ReadAsync(response.Content, 262144, ct), new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new();
        if (rows.Any(r => r.SecurityScope != invocation.SecurityScope))
            return ResourceResolutionResult.Unauthorized("RESOURCE_SCOPE_MISMATCH");

        return ResourceResolutionResult.Available(rows.Select(r => new ResourceDescriptor(r.Type, r.Id, r.SecurityScope, r.AuthorityDomain, r.Version, new Dictionary<string, string>(r.Attributes ?? new()) { ["role"] = registration.Role }, r.ObservedAt)).ToList());
    }

    private sealed record ResourceResponse(string Type, string Id, string SecurityScope, string AuthorityDomain, string? Version, Dictionary<string, string>? Attributes, DateTimeOffset ObservedAt);
}
