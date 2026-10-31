namespace Synentra.Infrastructure.Configuration.ResponseRetention;

public sealed class ResponseRetentionConfiguration : ISynentraConfiguration
{
    public static string SectionName => "ResponseRetention";

    public Dictionary<string, AuthenticatedResponsePolicy> AuthenticatedResponsePolicies { get; set; } = new(StringComparer.Ordinal);
}