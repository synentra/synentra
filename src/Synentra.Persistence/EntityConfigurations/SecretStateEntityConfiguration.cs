using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class SecretStateEntityConfiguration : IEntityTypeConfiguration<SecretStateEntity>
{
    public void Configure(EntityTypeBuilder<SecretStateEntity> e)
    {
        e.ToTable("SecretStates"); e.HasKey(x => x.Id); 
        e.Property(x => x.Id).ValueGeneratedNever(); 
        e.Property(x => x.Version).IsConcurrencyToken();
    }
}
