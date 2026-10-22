namespace Synentra.Infrastructure.Configuration.Persistence;

public class PersistenceProviders
{
    public SqliteConfiguration Sqlite { get; set; } = new();
    public PostgreSqlConfiguration Postgres { get; set; } = new();
}