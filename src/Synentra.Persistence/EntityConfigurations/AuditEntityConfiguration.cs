using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class AuditEntityConfiguration : IEntityTypeConfiguration<AuditEntity>
{
    public void Configure(EntityTypeBuilder<AuditEntity> e)
    {
        e.ToTable("Audits");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.HasIndex(x => new { x.SecurityScope, x.Timestamp });
        e.HasIndex(x => x.InvocationId);
    }
}