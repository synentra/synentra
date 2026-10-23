namespace Synentra.Infrastructure.Configuration.OutboundHttp;

public class OutboundHttpConfiguration: ISynentraConfiguration
{
    public static string SectionName => "OutboundHttp";

    public string[] AllowedDestinations { get; set; } = [];
    public bool? AllowPrivateAddresses { get; set; } = false;
    public int? MaxResponseBodySizeBytes { get; set; } = 2 * 1024 * 1024; // 2MB
    public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
