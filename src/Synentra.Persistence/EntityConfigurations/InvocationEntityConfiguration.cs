using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class InvocationEntityConfiguration : IEntityTypeConfiguration<InvocationEntity>
{
    public void Configure(EntityTypeBuilder<InvocationEntity> e)
    {
        e.ToTable("Invocations");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.Version).IsConcurrencyToken();
    }
}