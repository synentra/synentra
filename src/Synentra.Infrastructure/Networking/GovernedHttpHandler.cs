using System.Net;
using System.Net.Sockets;
using System.Security;
using Microsoft.Extensions.Options;
using Synentra.Infrastructure.Configuration;
using Synentra.Infrastructure.Configuration.OutboundHttp;

namespace Synentra.Infrastructure.Networking;

public sealed class GovernedHttpHandler(IOptions<OutboundHttpConfiguration> options) : DelegatingHandler(CreateSockets(options))
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var uri = request.RequestUri ?? throw new SecurityException("Missing destination.");

        if (uri.Scheme is not ("http" or "https") 
            || !string.IsNullOrEmpty(uri.UserInfo) 
            || !options.Value.AllowedDestinations.Contains(uri.GetLeftPart(UriPartial.Authority)))
            throw new SecurityException("Destination origin is not allowed.");

        // Host is derived from URI, never a caller-supplied header.
        request.Headers.Host = null;
        return base.SendAsync(request, ct);
    }

    private static SocketsHttpHandler CreateSockets(IOptions<OutboundHttpConfiguration> options) => new()
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false,
        ConnectTimeout = TimeSpan.FromSeconds(10),
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        ConnectCallback = async (context, ct) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
            if (addresses.Length == 0 || (!(options.Value.AllowPrivateAddresses ?? false) && addresses.Any(IsPrivate)))
                throw new SecurityException("Destination address rejected.");

            // Connect to the validated IP, preventing a second DNS lookup / rebinding race.
            foreach (var ip in addresses)
            {
                var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(ip, context.DnsEndPoint.Port), ct);
                    return new NetworkStream(socket, true);
                }
                catch (SocketException)
                {
                    socket.Dispose();
                }
            }

            throw new HttpRequestException("No validated destination address is reachable.");
        }
    };

    private static bool IsPrivate(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            return true;

        var b = address.GetAddressBytes();
        if (b.Length == 16)
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast || (b[0] & 0xfe) == 0xfc;

        return b[0] is 0 or 10 or 127 
            || b[0] >= 224 
            || (b[0] == 169 && b[1] == 254) 
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) 
            || (b[0] == 192 && b[1] == 168) 
            || (b[0] == 100 && b[1] >= 64 && b[1] <= 127);
    }
}
