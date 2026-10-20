using System.Text.Json;

namespace Synentra.Infrastructure.Features.Policies;

public static class JsonPointer
{
    public static bool TryRead(JsonElement root, string pointer, out JsonElement value)
    {
        value = root;
        if (pointer == "")
            return true;

        if (!pointer.StartsWith('/'))
            return false;

        foreach (var raw in pointer[1..].Split('/'))
        {
            for (int i = 0; i < raw.Length; i++)
                if (raw[i] == '~' && (i + 1 >= raw.Length || raw[++i] is not ('0' or '1')))
                    return false;

            var key = raw.Replace("~1", "/").Replace("~0", "~");
            if (value.ValueKind == JsonValueKind.Object)
            {
                if (!value.TryGetProperty(key, out value))
                    return false;
            }
            else if (value.ValueKind == JsonValueKind.Array && int.TryParse(key, out var n) && n >= 0 && n < value.GetArrayLength() && (key == "0" || !key.StartsWith('0')))
                value = value[n];
            else
                return false;
        }

        return true;
    }

    public static bool Equal(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind)
            return false;
        return a.ValueKind switch
        {
            JsonValueKind.Object => a.EnumerateObject().Count() == b.EnumerateObject().Count() && a.EnumerateObject().All(p => b.TryGetProperty(p.Name, out var v) && Equal(p.Value, v)),
            JsonValueKind.Array => a.GetArrayLength() == b.GetArrayLength() && a.EnumerateArray().Zip(b.EnumerateArray()).All(p => Equal(p.First, p.Second)),
            JsonValueKind.Number => a.TryGetDecimal(out var x) && b.TryGetDecimal(out var y) ? x == y : a.GetRawText() == b.GetRawText(),
            JsonValueKind.String => a.GetString() == b.GetString(),
            _ => a.ValueKind == b.ValueKind
        };
    }
}
