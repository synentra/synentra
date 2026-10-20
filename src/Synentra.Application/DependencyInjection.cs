using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Security;
using Synentra.Application.Common.Serialization;
using Synentra.Application.Features.Approvals;
using Synentra.Application.Features.Budgets;
using Synentra.Application.Features.Executions.Reconciliation;
using Synentra.Application.Features.Invocations.Admission;
using Synentra.Application.Features.Invocations.Evaluation;
using Synentra.Application.Features.Invocations.Normalization;
using Synentra.Application.Features.Invocations.Resume;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests;
using Synentra.Application.Features.TaskGrants;

namespace Synentra.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSynentraApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        foreach (var implementation in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters))
        {
            foreach (var contract in implementation.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IActionHandler<,>)))
                services.TryAddScoped(contract, implementation);
        }

        services.AddValidatorsFromAssembly(assembly);
        services.TryAddScoped<ApprovalService>();
        services.TryAddScoped<TaskGrantService>();
        services.TryAddScoped<ManifestManager>();
        services.TryAddScoped<BudgetSettlementService>();
        services.TryAddScoped<ResumeService>();
        services.TryAddScoped<ReconciliationService>();
        services.TryAddScoped<BundleValidator>();
        services.TryAddScoped<IManifestManager>(sp => sp.GetRequiredService<ManifestManager>());
        services.TryAddScoped<CanonicalJsonService>();
        services.TryAddScoped<AuthorizationService>();
        services.TryAddScoped<InvocationNormalizer>();
        services.TryAddScoped<DecisionEngine>();
        services.TryAddScoped<IInvocationEvaluator>(sp => sp.GetRequiredService<DecisionEngine>());
        services.TryAddScoped<AdmissionService>();
        services.TryAddScoped<IExecutionAdmission>(sp => sp.GetRequiredService<AdmissionService>());
        return services;
    }
}
