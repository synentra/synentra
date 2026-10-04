using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synentra.Infrastructure.Persistence.Common.Exceptions;
using Synentra.Infrastructure.Persistence.PostgreSQL.Contexts;
using Synentra.Infrastructure.Persistence.PostgreSQL.Services;
using Synentra.Infrastructure.Persistence.PostgreSQL.UnitTests.Helpers;

namespace Synentra.Infrastructure.Persistence.PostgreSQL.UnitTests.Services;

public class PostgresqlDatabaseInitializerTests
{
    [Fact]
    public async Task EnsureDatabaseCreatedAsync_NewDatabase_LogsCreated()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = PostgresqlTestContextFactory.CreateFactory(dbName);

        var initializer = new PostgresqlDatabaseInitializer(factory, NullLogger<PostgresqlDatabaseInitializer>.Instance);

        Func<Task> act = () => initializer.EnsureDatabaseCreatedAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_ExistingDatabase_DoesNotThrow()
    {
        var dbName = Guid.NewGuid().ToString();

        // First call creates the DB
        var factory1 = PostgresqlTestContextFactory.CreateFactory(dbName);
        await new PostgresqlDatabaseInitializer(factory1, NullLogger<PostgresqlDatabaseInitializer>.Instance)
            .EnsureDatabaseCreatedAsync();

        // Second call should still succeed
        var factory2 = PostgresqlTestContextFactory.CreateFactory(dbName);
        var initializer2 = new PostgresqlDatabaseInitializer(factory2, NullLogger<PostgresqlDatabaseInitializer>.Instance);

        Func<Task> act = () => initializer2.EnsureDatabaseCreatedAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_FactoryThrows_ThrowsDatabaseInitializerException()
    {
        var factory = new ThrowingContextFactory();
        var initializer = new PostgresqlDatabaseInitializer(factory, NullLogger<PostgresqlDatabaseInitializer>.Instance);

        Func<Task> act = () => initializer.EnsureDatabaseCreatedAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DatabaseInitializerException>();
    }

    private sealed class ThrowingContextFactory : IDbContextFactory<PostgresqlApplicationContext>
    {
        public PostgresqlApplicationContext CreateDbContext() => throw new InvalidOperationException("Connection failed");
        public Task<PostgresqlApplicationContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Connection failed");
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_WithCancellationToken_DoesNotThrow()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = PostgresqlTestContextFactory.CreateFactory(dbName);

        var initializer = new PostgresqlDatabaseInitializer(factory, NullLogger<PostgresqlDatabaseInitializer>.Instance);
        using var cts = new CancellationTokenSource();

        Func<Task> act = () => initializer.EnsureDatabaseCreatedAsync(cts.Token);

        await act.Should().NotThrowAsync();
    }
}
