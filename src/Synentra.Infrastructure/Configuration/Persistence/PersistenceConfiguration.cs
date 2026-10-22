namespace Synentra.Infrastructure.Configuration.Persistence;

public class PersistenceConfiguration
{
    public string DefaultProvider { get; set; } = "Sqlite";
    public PersistenceProviders Providers { get; set; } = new();
}