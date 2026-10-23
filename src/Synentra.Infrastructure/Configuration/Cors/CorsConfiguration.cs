namespace Synentra.Infrastructure.Configuration.Cors;

public class CorsConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Cors";

    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = [];
    public string[] AllowedHeaders { get; set; } = [];
}