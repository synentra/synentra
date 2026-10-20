using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class ExecutionEntityConfiguration : IEntityTypeConfiguration<ExecutionEntity>
{
    public void Configure(EntityTypeBuilder<ExecutionEntity> e)
    {
        e.ToTable("Executions");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.Version).IsConcurrencyToken();
        e.HasIndex(x => x.InvocationId).IsUnique();
    }
}