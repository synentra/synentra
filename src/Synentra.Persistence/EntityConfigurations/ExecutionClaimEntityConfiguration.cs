using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class ExecutionClaimEntityConfiguration : IEntityTypeConfiguration<ExecutionClaimEntity>
{
    public void Configure(EntityTypeBuilder<ExecutionClaimEntity> e)
    {
        e.ToTable("ExecutionClaims");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.Version).IsConcurrencyToken();
        e.HasIndex(x => x.InvocationId).IsUnique();
    }
}