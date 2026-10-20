using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class ManifestEntityConfiguration : IEntityTypeConfiguration<ManifestEntity>
{
    public void Configure(EntityTypeBuilder<ManifestEntity> e)
    {
        e.ToTable("Manifests");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.HasIndex(x => new { x.ManifestId, x.Revision }).IsUnique();
    }
}