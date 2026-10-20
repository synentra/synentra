using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class IdempotencyEntityConfiguration : IEntityTypeConfiguration<IdempotencyEntity>
{
    public void Configure(EntityTypeBuilder<IdempotencyEntity> e)
    {
        e.ToTable("Idempotencies");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.Version).IsConcurrencyToken();
        e.HasIndex(x => x.ScopeKey).IsUnique();
    }
}