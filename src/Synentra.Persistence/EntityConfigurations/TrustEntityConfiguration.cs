using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class TrustEntityConfiguration : IEntityTypeConfiguration<TrustEntity>
{
    public void Configure(EntityTypeBuilder<TrustEntity> e)
    {
        e.ToTable("Trusts");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.Version).IsConcurrencyToken();
        e.HasIndex(x => new { x.AgentId, x.SecurityScope }).IsUnique();
    }
}