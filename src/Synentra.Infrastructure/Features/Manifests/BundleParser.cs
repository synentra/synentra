using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Models;
using Synentra.Application.Features.Manifests.Exceptions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Synentra.Infrastructure.Features.Manifests;

public sealed class BundleParser : IBundleParser
{
    public const int MaximumBytes = 512 * 1024;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 64,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public GovernanceBundleModel Parse(Stream stream)
    {
        try
        {
            using var buffer = new MemoryStream();
            var bytes = new byte[8192];
            int count;
            while ((count = stream.Read(bytes)) > 0)
            {
                if (buffer.Length + count > MaximumBytes)
                    throw Invalid("SIZE_EXCEEDED");

                buffer.Write(bytes, 0, count);
            }

            var text = new UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('﻿');
            if (!text.TrimStart().StartsWith('{'))
                text = YamlToJson(text);

            using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 64 });
            CheckKeys(doc.RootElement);
            var model = JsonSerializer.Deserialize<GovernanceBundleModel>(text, Options) ?? throw Invalid("EMPTY_BUNDLE");

            if (model.ApiVersion != "synentra.io/v1alpha2" || model.Kind != "GovernanceBundle")
                throw Invalid("UNSUPPORTED_BUNDLE_VERSION");

            if (string.IsNullOrWhiteSpace(model.Metadata?.Name) || string.IsNullOrWhiteSpace(model.Metadata.Version) || model.Spec is null)
                throw Invalid("MISSING_BUNDLE_METADATA");

            return model;
        }
        catch (BundleParseException)
        {
            throw;
        }
        catch (Exception ex)when (ex is JsonException or YamlException or DecoderFallbackException or InvalidOperationException or ArgumentException)
        {
            throw Invalid("INVALID_BUNDLE_SYNTAX");
        }
    }

    private static BundleParseException Invalid(string code) => new("Bundle rejected.", new[] { code });
    private static void CheckKeys(JsonElement e)
    {
        if (e.ValueKind == JsonValueKind.Object)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in e.EnumerateObject())
            {
                if (!seen.Add(property.Name))
                    throw Invalid("DUPLICATE_KEY");

                CheckKeys(property.Value);
            }
        }
        else if (e.ValueKind == JsonValueKind.Array)
            foreach (var item in e.EnumerateArray())
                CheckKeys(item);
    }

    private static string YamlToJson(string text)
    {
        var parser = new Parser(new StringReader(text));
        int depth = 0, documents = 0;
        while (parser.MoveNext())
        {
            var e = parser.Current;
            if (e is AnchorAlias)
                throw Invalid("YAML_ALIAS_REJECTED");

            if (e is NodeEvent node && (!node.Anchor.IsEmpty || !node.Tag.IsEmpty))
                throw Invalid("YAML_TAG_OR_ANCHOR_REJECTED");

            if (e is DocumentStart && ++documents > 1)
                throw Invalid("MULTIPLE_DOCUMENTS");

            if (e is MappingStart or SequenceStart)
            {
                if (++depth > 64)
                    throw Invalid("DEPTH_EXCEEDED");
            }

            if (e is MappingEnd or SequenceEnd)
                depth--;
        }

        var yaml = new YamlStream();
        yaml.Load(new StringReader(text));
        if (yaml.Documents.Count != 1)
            throw Invalid("EMPTY_BUNDLE");

        return JsonSerializer.Serialize(ConvertNode(yaml.Documents[0].RootNode));
    }

    private static object? ConvertNode(YamlNode node)
    {
        if (node is YamlMappingNode map)
        {
            var result = new Dictionary<string, object?>();
            foreach (var pair in map.Children)
            {
                if (pair.Key is not YamlScalarNode key || key.Value is null || key.Value == "<<" || result.ContainsKey(key.Value))
                    throw Invalid("INVALID_MAPPING_KEY");

                result.Add(key.Value, ConvertNode(pair.Value));
            }

            return result;
        }

        if (node is YamlSequenceNode sequence)
            return sequence.Children.Select(ConvertNode).ToArray();

        if (node is not YamlScalarNode scalar)
            throw Invalid("INVALID_YAML_NODE");

        var value = scalar.Value;
        if (scalar.Style != ScalarStyle.Plain)
            return value ?? "";

        if (value is null or "null" or "~")
            return null;

        if (value == "true")
            return true;

        if (value == "false")
            return false;

        // JSON-compatible numeric grammar only; quoted values remain strings.
        if (value.Length > 0 && (char.IsDigit(value[0]) || value[0] == '-'))
        {
            try
            {
                using var d = JsonDocument.Parse(value);
                if (d.RootElement.ValueKind == JsonValueKind.Number)
                    return d.RootElement.Clone();
            }
            catch (JsonException)
            {
            }
        }

        return value;
    }
}
