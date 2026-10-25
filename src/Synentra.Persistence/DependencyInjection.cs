using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synentra.Application.Common.Persistence;
using Synentra.Application.Common.Messaging;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Application.Features.TaskGrants.Contracts;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Invocations.Admission;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Budgets.Contracts;
using Synentra.Persistence.Serialization;
using Synentra.Persistence.Transactions;
using Synentra.Persistence.Features.Approvals;
using Synentra.Persistence.Features.TaskGrants;
using Synentra.Persistence.Features.Invocations;
using Synentra.Persistence.Features.Executions;
using Synentra.Persistence.Features.Budgets;
using Synentra.Persistence.Features.Auditing;
using Synentra.Persistence.Features.Manifests;
using Synentra.Persistence.Features.Messaging;
using Synentra.Persistence.Features.Security;

namespace Synentra.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddSynentraPersistence(this IServiceCollection services)
    {
        services.TryAddScoped<EfUnitOfWork>();
        services.TryAddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EfUnitOfWork>());
        services.TryAddScoped<IDbContextScope>(sp => sp.GetRequiredService<EfUnitOfWork>());
        services.TryAddSingleton<PayloadCodec>();
        services.AddScoped<IApprovalStore, ApprovalStore>();
        services.AddScoped<IGrantStore, GrantStore>();
        services.AddScoped<IInvocationStore, InvocationStore>();
        services.AddScoped<IExecutionRecordStore, ExecutionRecordStore>();
        services.AddScoped<IExecutionClaimStore, ExecutionClaimStore>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IAuditStore, AuditStore>();
        services.AddScoped<IManifestStore, ManifestStore>();
        services.AddScoped<IOutbox, OutboxStore>();
        services.AddScoped<BudgetStore>();
        services.AddScoped<IBudgetStore>(sp => sp.GetRequiredService<BudgetStore>());
        services.AddScoped<CredentialBindingStore>();
        services.AddScoped<SecretStateStore>();
        services.AddScoped<OutcomeEvidenceStore>();
        services.AddScoped<IOutcomeEvidenceStore>(s => s.GetRequiredService<OutcomeEvidenceStore>());
        return services;
    }
}
