using Microsoft.EntityFrameworkCore;

namespace Synentra.Persistence.EntityConfigurations;

internal static class SynentraModelBuilderExtensions
{
    public static ModelBuilder ApplySynentraConfigurations(this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new ApprovalEntityConfiguration());
        builder.ApplyConfiguration(new GrantEntityConfiguration());
        builder.ApplyConfiguration(new InvocationEntityConfiguration());
        builder.ApplyConfiguration(new ExecutionEntityConfiguration());
        builder.ApplyConfiguration(new ExecutionClaimEntityConfiguration());
        builder.ApplyConfiguration(new IdempotencyEntityConfiguration());
        builder.ApplyConfiguration(new AuditEntityConfiguration());
        builder.ApplyConfiguration(new ManifestEntityConfiguration());
        builder.ApplyConfiguration(new ActiveManifestEntityConfiguration());
        builder.ApplyConfiguration(new OutboxEntityConfiguration());
        builder.ApplyConfiguration(new TrustEntityConfiguration());
        builder.ApplyConfiguration(new TrustEvidenceEntityConfiguration());
        builder.ApplyConfiguration(new QuarantineEntityConfiguration());
        builder.ApplyConfiguration(new CredentialBindingEntityConfiguration());
        builder.ApplyConfiguration(new SecretEntityConfiguration());
        builder.ApplyConfiguration(new OutcomeEvidenceEntityConfiguration());

        return builder;
    }
}