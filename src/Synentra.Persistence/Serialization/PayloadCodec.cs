using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Synentra.Persistence.Serialization;

public sealed class PayloadCodec(IDataProtectionProvider protection)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new ReadOnlyStringSetConverter(),
            new ResourceDescriptorConverter(),
            new OutcomeEvidenceConverter()
        }
    };

    private readonly IDataProtector protector = protection.CreateProtector("Synentra.Persistence.Payload.v1");

    public string Encode<T>(T value) 
        => protector.Protect(JsonSerializer.Serialize(value, Json));

    public T Decode<T>(string value) => JsonSerializer.Deserialize<T>(protector.Unprotect(value), Json) 
        ?? throw new InvalidDataException("Stored payload is null.");

    public T FromJson<T>(string value) 
        => JsonSerializer.Deserialize<T>(value, Json) ?? throw new InvalidDataException("Stored payload is null.");
}
