namespace Synentra.Infrastructure.Networking;

public static class BoundedResponse
{
    public static async Task<byte[]> ReadAsync(HttpContent content, int maximumBytes, CancellationToken ct)
    {
        if (content.Headers.ContentLength > maximumBytes)
            throw new InvalidDataException("Response limit exceeded.");

        await using var stream = await content.ReadAsStreamAsync(ct);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        int count;

        while ((count = await stream.ReadAsync(buffer, ct)) > 0)
        {
            if (output.Length + count > maximumBytes)
                throw new InvalidDataException("Response limit exceeded.");

            output.Write(buffer, 0, count);
        }

        return output.ToArray();
    }
}
