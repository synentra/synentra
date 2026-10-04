using Microsoft.EntityFrameworkCore;
using Synentra.Infrastructure.Persistence.PostgreSQL.Contexts;

namespace Synentra.Infrastructure.Persistence.PostgreSQL.UnitTests.Helpers;

internal static class PostgresqlTestContextFactory
{
    public static PostgresqlApplicationContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<PostgresqlApplicationContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new PostgresqlApplicationContext(options);
    }

    public static IDbContextFactory<PostgresqlApplicationContext> CreateFactory(string? databaseName = null)
    {
        var dbName = databaseName ?? Guid.NewGuid().ToString();
        return new InMemoryContextFactory(dbName);
    }

    private sealed class InMemoryContextFactory(string databaseName)
        : IDbContextFactory<PostgresqlApplicationContext>
    {
        public PostgresqlApplicationContext CreateDbContext() => Create(databaseName);

        public Task<PostgresqlApplicationContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Create(databaseName));
    }
}
