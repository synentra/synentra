using Microsoft.EntityFrameworkCore;
using Synentra.Infrastructure.Persistence.Common;
using Synentra.Infrastructure.Persistence.Common.Exceptions;

namespace Synentra.Infrastructure.Persistence.PostgreSQL.Contexts;

public class PostgresqlApplicationContext : BaseDbContext
{
    public PostgresqlApplicationContext(
        DbContextOptions<PostgresqlApplicationContext> contextOptions)
        : base(contextOptions)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        try
        {
            base.OnModelCreating(modelBuilder);
            ApplyEntityConfigurations(modelBuilder);
        }
        catch (Exception ex)
        {
            throw new DatabaseModelCreatingException(ex);
        }
    }

    private static void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PostgresqlApplicationContext).Assembly);
    }
}
