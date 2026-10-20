using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synentra.Persistence.Entities;

namespace Synentra.Persistence.EntityConfigurations;

public sealed class OutcomeEvidenceEntityConfiguration : IEntityTypeConfiguration<OutcomeEvidenceEntity>
{
    public void Configure(EntityTypeBuilder<OutcomeEvidenceEntity> e)
    {
        e.ToTable("OutcomeEvidences");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.HasIndex(x => new { x.ExecutionId, x.ObservedAt });
    }
}