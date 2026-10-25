using Microsoft.EntityFrameworkCore;
using Synentra.Persistence.Entities;
using Synentra.Persistence.EntityConfigurations;

namespace Synentra.Persistence;

public abstract class SynentraDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<ActiveManifestEntity> ActiveManifests { get; set; }
    public DbSet<ApprovalEntity> Approvals { get; set; }
    public DbSet<AuditEntity> Audits { get; set; }
    public DbSet<BudgetAccountEntity> BudgetAccounts { get; set; }
    public DbSet<CredentialBindingEntity> CredentialBindings { get; set; }
    public DbSet<ExecutionClaimEntity> ExecutionClaims { get; set; }
    public DbSet<ExecutionEntity> Executions { get; set; }
    public DbSet<GrantEntity> Grants { get; set; }
    public DbSet<IdempotencyEntity> Idempotencies { get; set; }
    public DbSet<InvocationEntity> Invocations { get; set; }
    public DbSet<ManifestEntity> Manifests { get; set; }
    public DbSet<OutboxEntity> Outboxes { get; set; }
    public DbSet<OutcomeEvidenceEntity> OutcomeEvidences { get; set; }
    public DbSet<QuarantineEntity> Quarantines { get; set; }
    public DbSet<ReservationEntity> Reservations { get; set; }
    public DbSet<SecretEntity> Secrets { get; set; }
    public DbSet<SecretStateEntity> SecretStates { get; set; }
    public DbSet<TrustEntity> Trusts { get; set; }
    public DbSet<TrustEvidenceEntity> TrustEvidences { get; set; }

    protected override void OnModelCreating(ModelBuilder builder) => builder.ApplySynentraConfigurations();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default) => SaveChangesAsync(true, ct);
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        PrepareChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private void PrepareChanges()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var version = entry.Metadata.FindProperty("Version");
            if (version is not null)
                entry.Property("Version").CurrentValue = checked((long)(entry.Property("Version").CurrentValue ?? 0L) + 1);
        }

        if (ChangeTracker.Entries<AuditEntity>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit entries are append-only.");
    }
}
