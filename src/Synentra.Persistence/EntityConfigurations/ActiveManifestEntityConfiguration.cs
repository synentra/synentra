using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class ActiveManifestEntityConfiguration : IEntityTypeConfiguration<ActiveManifestEntity>
{
    public void Configure(EntityTypeBuilder<ActiveManifestEntity> e)
    {
        e.ToTable("ActiveManifests");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.Version).IsConcurrencyToken();
        e.HasData(new ActiveManifestEntity { Id = 1, Version = 0 });
    }
}