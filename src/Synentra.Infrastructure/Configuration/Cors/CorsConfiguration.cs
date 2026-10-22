namespace Synentra.Infrastructure.Configuration.Cors;

public class CorsConfiguration
{
    public string[] AllowedOrigins { get; set; } = ["https://localhost:7181"];
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"];
    public string[] AllowedHeaders { get; set; } = ["Content-Type", "Authorization"];
}