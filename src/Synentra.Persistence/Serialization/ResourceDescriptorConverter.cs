using System.Text.Json;
using System.Text.Json.Serialization;
using Synentra.Domain.Resources.ValueObjects;

namespace Synentra.Persistence.Serialization;

internal sealed class ResourceDescriptorConverter : JsonConverter<ResourceDescriptor>
{
    public override ResourceDescriptor Read(
        ref Utf8JsonReader reader, 
        Type type, 
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var e = doc.RootElement;

        return new(
            e.GetProperty("type").GetString()!, 
            e.GetProperty("id").GetString()!, 
            e.GetProperty("securityScope").GetString()!, 
            e.GetProperty("authorityDomain").GetString()!, 
            e.GetProperty("version").GetString(), 
            e.GetProperty("trustedAttributes").Deserialize<Dictionary<string, string>>(options), 
            e.GetProperty("observedAt").GetDateTimeOffset());
    }

    public override void Write(
        Utf8JsonWriter writer, 
        ResourceDescriptor value, 
        JsonSerializerOptions options) 
        => JsonSerializer.Serialize(writer, 
            new {
                value.Type, 
                value.Id, 
                value.SecurityScope, 
                value.AuthorityDomain, 
                value.Version, 
                value.TrustedAttributes, 
                value.ObservedAt }, 
            options);
}
