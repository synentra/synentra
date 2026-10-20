using Synentra.Domain.Invocations.ValueObjects;
using Synentra.Domain.Invocations;
using Synentra.Domain.Resources.ValueObjects;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;

namespace Synentra.Application.Common.Serialization;

public sealed class CanonicalJsonService
{
    public BindingDigest ComputeDigest(ToolInvocation invocation)
    {
        var sb = new StringBuilder(1024);
        sb.Append("v1|");
        sb.Append(invocation.AuthenticatedAgentId).Append('|');
        sb.Append(invocation.SecurityScope).Append('|');
        sb.Append(invocation.OperationId.Value).Append('|');
        sb.Append(invocation.OperationVersion).Append('|');
        sb.Append(invocation.BindingId).Append('|');
        sb.Append(invocation.BindingVersion).Append('|');
        sb.Append(invocation.TargetId).Append('|');
        sb.Append(invocation.TaskGrantId ?? "").Append('|');
        AppendCanonical(sb, invocation.Arguments);
        sb.Append('|');
        AppendCanonicalResources(sb, invocation.Resources);
        sb.Append('|');
        AppendCanonicalContext(sb, invocation.Context);
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = SHA256.HashData(bytes);
        return new BindingDigest(Convert.ToHexString(hash));
    }

    public string CanonicalizeArgumentDigest(IReadOnlyDictionary<string, object> args)
    {
        var sb = new StringBuilder();
        AppendCanonical(sb, args);
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static void AppendCanonical(StringBuilder sb, object? value)
    {
        switch (value)
        {
            case null:
                sb.Append("null");
                return;
            case string s:
                sb.Append('"').Append(s).Append('"');
                return;
            case bool b:
                sb.Append(b ? "true" : "false");
                return;
            case int i:
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                return;
            case long l:
                sb.Append(l.ToString(CultureInfo.InvariantCulture));
                return;
            case short sh:
                sb.Append(sh.ToString(CultureInfo.InvariantCulture));
                return;
            case byte bt:
                sb.Append(bt.ToString(CultureInfo.InvariantCulture));
                return;
            case decimal d:
                sb.Append(d.ToString(CultureInfo.InvariantCulture));
                return;
            case double dbl:
                sb.Append(dbl.ToString("R", CultureInfo.InvariantCulture));
                return;
            case float f:
                sb.Append(f.ToString("R", CultureInfo.InvariantCulture));
                return;
            case JsonElement je:
                sb.Append(je.GetRawText());
                return;
            case IReadOnlyDictionary<string, object> map:
                sb.Append('{');
                var first = true;
                foreach (var kv in map.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    if (!first)
                        sb.Append(',');
                    first = false;
                    sb.Append('"').Append(kv.Key).Append("\":");
                    AppendCanonical(sb, kv.Value);
                }

                sb.Append('}');
                return;
            case System.Collections.IEnumerable seq:
                sb.Append('[');
                first = true;
                foreach (var item in seq)
                {
                    if (!first)
                        sb.Append(',');
                    first = false;
                    AppendCanonical(sb, item);
                }

                sb.Append(']');
                return;
            default:
                sb.Append('"').Append(value.ToString()).Append('"');
                return;
        }
    }

    private static void AppendCanonicalResources(StringBuilder sb, IReadOnlyList<ResourceDescriptor> resources)
    {
        sb.Append('[');
        var ordered = resources.OrderBy(r => r.CanonicalKey, StringComparer.Ordinal).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(ordered[i].CanonicalKey);
            if (ordered[i].Version is { } v)
                sb.Append("@{").Append(v).Append('}');
        }

        sb.Append(']');
    }

    private static void AppendCanonicalContext(StringBuilder sb, IReadOnlyDictionary<string, string> ctx)
    {
        // Only include context keys relevant to the digest — exclude untrusted intent/workflow IDs
        // (workflowId, declaredPurpose) are intentionally excluded
        sb.Append('{');
        var first = true;
        foreach (var kv in ctx.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (kv.Key is "workflowId" or "declaredPurpose")
                continue;
            if (!first)
                sb.Append(',');
            first = false;
            sb.Append('"').Append(kv.Key).Append("\":\"").Append(kv.Value).Append('"');
        }

        sb.Append('}');
    }
}
