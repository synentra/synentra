namespace Synentra.Infrastructure.Configuration.OutboundHttp;

public class OutboundHttpConfiguration
{
    public string[] AllowedDestinations { get; set; } = ["https://localhost:7181"];
    public bool? AllowPrivateAddresses { get; set; } = false;
    public int? MaxResponseBodySizeBytes { get; set; } = 2 * 1024 * 1024; // 2MB
    public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
