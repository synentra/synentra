using Synentra.Application.Common.Security;
using Synentra.Infrastructure.Configuration.ResponseRetention;
using System.Text;
using System.Text.Json.Nodes;

namespace Synentra.Infrastructure.Features.Secrets;

public static class CredentialResponseRetention
{
    public static string? Apply(string? payload, string target, ResolvedCredential? credential, ResponseRetentionConfiguration options)
    {
        if (credential is null) 
            return payload;

        if (!options.AuthenticatedResponsePolicies.TryGetValue(target, out var policy)) 
            return null;

        policy.Validate();

        if (policy.Mode == "omit" || payload is null) 
            return null;

        try
        {
            if (JsonNode.Parse(payload) is not JsonObject source) return null;
            var variants = Variants(credential);
            var result = new JsonObject();
            foreach (var name in policy.AllowedProperties)
                if (!SensitiveName(name) && !variants.Any(v => name.Contains(v, StringComparison.Ordinal)) && source.TryGetPropertyValue(name, out var value)) 
                    result[name] = Clean(value, variants);

            return result.ToJsonString();
        }
        catch (System.Text.Json.JsonException) { return null; }
    }

    private static bool SensitiveName(string name) 
        => new[] { "authorization", "password", "secret", "token", "apikey", "api_key", "credential", "cookie" }
        .Any(part => name.Contains(part, StringComparison.OrdinalIgnoreCase));

    private static JsonNode? Clean(JsonNode? node, string[] variants)
    {
        if (node is JsonObject obj)
        {
            var result = new JsonObject();
            foreach (var item in obj) if (!SensitiveName(item.Key) && !variants.Any(v => item.Key.Contains(v, StringComparison.Ordinal))) result[item.Key] = Clean(item.Value, variants);
            return result;
        }

        if (node is JsonArray array) 
            return new JsonArray(array.Select(x => Clean(x, variants)).ToArray());

        if (node is JsonValue scalar && scalar.TryGetValue<string>(out var value))
        {
            foreach (var variant in variants.Distinct().OrderByDescending(x => x.Length)) value = value.Replace(variant, "[REDACTED]", StringComparison.Ordinal);
            return JsonValue.Create(value);
        }

        if (node is JsonValue numeric && variants.Any(v => numeric.ToJsonString().Contains(v, StringComparison.Ordinal))) 
            return JsonValue.Create("[REDACTED]");

        return node?.DeepClone();
    }
    private static string[] Variants(ResolvedCredential credential)
    {
        var raw = new List<string> { credential.Value };
        if (credential.Scheme == "Basic")
        {
            try
            {
                var decoded = new UTF8Encoding(false, true).GetString(Convert.FromBase64String(credential.Value));
                raw.Add(decoded);
                if (decoded.IndexOf(':') is var colon && colon >= 0) raw.Add(decoded[(colon + 1)..]);
            }
            catch (Exception ex) when (ex is FormatException or DecoderFallbackException) { }
        }

        return raw
            .Where(x => !string.IsNullOrEmpty(x))
            .SelectMany(x => new[] { x, Uri.EscapeDataString(x), Convert.ToBase64String(Encoding.UTF8.GetBytes(x)) })
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(x => x.Length)
            .ToArray();
    }
}


