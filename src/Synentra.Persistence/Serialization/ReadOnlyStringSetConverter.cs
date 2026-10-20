using System.Text.Json;
using System.Text.Json.Serialization;

namespace Synentra.Persistence.Serialization;

internal sealed class ReadOnlyStringSetConverter : JsonConverter<IReadOnlySet<string>>
{
    public override IReadOnlySet<string> Read(
        ref Utf8JsonReader reader, 
        Type type, 
        JsonSerializerOptions options) 
        => JsonSerializer.Deserialize<HashSet<string>>(ref reader, options) ?? throw new JsonException("A set cannot be null.");

    public override void Write(
        Utf8JsonWriter writer, 
        IReadOnlySet<string> value, 
        JsonSerializerOptions options) 
        => JsonSerializer.Serialize(writer, value.OrderBy(x => x, StringComparer.Ordinal).ToArray(), options);
}
