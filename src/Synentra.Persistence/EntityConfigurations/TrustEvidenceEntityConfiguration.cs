using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class TrustEvidenceEntityConfiguration : IEntityTypeConfiguration<TrustEvidenceEntity>
{
    public void Configure(EntityTypeBuilder<TrustEvidenceEntity> e)
    {
        e.ToTable("TrustEvidences");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.HasIndex(x => new { x.AgentId, x.SecurityScope, x.EvidenceId }).IsUnique();
    }
}