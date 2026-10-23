namespace Synentra.Infrastructure.Configuration.Persistence;

public class PersistenceConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Persistence";

    public string DefaultProvider { get; set; } = "Sqlite";
    public PersistenceProviders Providers { get; set; } = new();
}