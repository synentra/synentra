using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Synentra.Persistence.Common;

public static class Keys
{
    public static string Of(params object? [] parts) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(parts))));
    public static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
