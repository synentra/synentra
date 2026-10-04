using Microsoft.EntityFrameworkCore;
using Synentra.Domain.Agents;
using Synentra.Infrastructure.Persistence.Common;
using Synentra.Infrastructure.Persistence.Common.EntityConfigurations;
using Synentra.Infrastructure.Persistence.Common.Exceptions;

namespace Synentra.Infrastructure.Persistence.Sqlite.Contexts;

public class SqliteApplicationContext : BaseDbContext
{
    public SqliteApplicationContext(
        DbContextOptions<SqliteApplicationContext> contextOptions)
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
        // Apply shared entity configurations from Common project
        modelBuilder.ApplyConfiguration(new AgentConfiguration());
        modelBuilder.ApplyConfiguration(new AgentHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new AuditTrailConfiguration());

        modelBuilder.Entity<Agent>()
            .Property(e => e.Status)
            .HasColumnType("TEXT");
    }
}