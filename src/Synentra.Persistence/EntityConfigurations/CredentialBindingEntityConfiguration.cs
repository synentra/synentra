using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class CredentialBindingEntityConfiguration : IEntityTypeConfiguration<CredentialBindingEntity>
{
    public void Configure(EntityTypeBuilder<CredentialBindingEntity> e)
    {
        e.ToTable("CredentialBindings");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.Version).IsConcurrencyToken();
    }
}