using System.Text.Json;
using System.Text.Json.Nodes;
using Synentra.Application.Features.Manifests.Models;
using Synentra.Domain.Resources.ValueObjects;
using Synentra.Infrastructure.Features.Policies;

namespace Synentra.Infrastructure.Features.Adapters;

public static class ArgumentMapper
{
    public static IReadOnlyDictionary<string, object> Map(IReadOnlyDictionary<string, object> arguments, Dictionary<string, MappingEntryModel>? mappings, IReadOnlyList<ResourceDescriptor> resources)
    {
        if (mappings is null || mappings.Count == 0)
            return arguments;

        var input = JsonSerializer.SerializeToElement(arguments);
        var output = new JsonObject();

        foreach (var (destination, mapping) in 
            mappings.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var sourceCount = 
                (mapping.InputPointer is null ? 0 : 1) + 
                (mapping.TargetPointer is null ? 0 : 1) + 
                (mapping.ResourceField is null ? 0 : 1) + 
                (mapping.Constant is null ? 0 : 1);

            if (sourceCount != 1)
            {
                throw new InvalidOperationException(
                    $"Mapping destination '{destination}' must have exactly one source.");
            }

            JsonElement value;

            if ((mapping.InputPointer ?? mapping.TargetPointer) is { } pointer)
            {
                if (!JsonPointer.TryRead(input, pointer, out value))
                {
                    throw new InvalidOperationException(
                        $"Mapping source '{pointer}' for destination '{destination}' " +
                        "was not found in the canonical arguments.");
                }
            }
            else if (mapping.ResourceField is { } field)
            {
                var matches = resources
                    .Where(r =>
                        r.TrustedAttributes.GetValueOrDefault("role")
                        == mapping.Role)
                    .ToArray();

                if (matches.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"Resource mapping for destination '{destination}' " +
                        $"requires exactly one resource with role '{mapping.Role}'.");
                }

                value = JsonSerializer.SerializeToElement(field switch
                {
                    "id" => matches[0].Id,
                    "version" => matches[0].Version,
                    "type" => matches[0].Type,
                    "securityScope" => matches[0].SecurityScope,
                    _ => matches[0].TrustedAttributes
                        .GetValueOrDefault(field)
                        ?? throw new InvalidOperationException(
                            $"Resource field '{field}' for destination " +
                            $"'{destination}' was not found.")
                });
            }
            else
            {
                value = JsonSerializer.SerializeToElement(mapping.Constant);
            }

            if (!destination.StartsWith('/') || destination == "/")
            {
                throw new InvalidOperationException(
                    $"Mapping destination '{destination}' must be a non-empty JSON pointer.");
            }

            var parts = destination[1..]
                .Split('/')
                .Select(x => x
                    .Replace("~1", "/")
                    .Replace("~0", "~"))
                .ToArray();

            JsonObject current = output;

            foreach (var part in parts[..^1])
            {
                if (current[part] is null)
                    current[part] = new JsonObject();

                current = current[part] as JsonObject
                    ?? throw new InvalidOperationException(
                        $"Mapping destination '{destination}' overlaps another mapping.");
            }

            if (current.ContainsKey(parts[^1]))
            {
                throw new InvalidOperationException(
                    $"Mapping destination '{destination}' overlaps another mapping.");
            }

            current[parts[^1]] = JsonNode.Parse(value.GetRawText());
        }

        return JsonSerializer.Deserialize<Dictionary<string, object>>(output.ToJsonString())!;
    }
}
