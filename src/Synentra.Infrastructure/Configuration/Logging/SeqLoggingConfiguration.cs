namespace Synentra.Infrastructure.Configuration.Logging;

public sealed record SeqLoggingConfiguration
{
    public bool Enabled { get; set; } = false;
    public string LogLevel { get; init; } = "Information";
    public string? ApiKey { get; init; }
    public string? Url { get; init; }

    public static SeqLoggingConfiguration Create()
    {
        return new SeqLoggingConfiguration
        {
            LogLevel = "Information"
        };
    }
}