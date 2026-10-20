using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class QuarantineEntityConfiguration : IEntityTypeConfiguration<QuarantineEntity>
{
    public void Configure(EntityTypeBuilder<QuarantineEntity> e)
    {
        e.ToTable("Quarantines");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.Version).IsConcurrencyToken();
        e.HasIndex(x => new { x.AgentId, x.SecurityScope, x.ReleasedAt });
    }
}