using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using NSubstitute;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Messaging;
using Synentra.Application.Common.Persistence;
using Synentra.Application.Common.Plugins;
using Synentra.Application.Common.Security;
using Synentra.Application.Common.Serialization;
using Synentra.Application.Features.Approvals;
using Synentra.Application.Features.Approvals.Commands.ApproveInvocation;
using Synentra.Application.Features.Approvals.Contracts;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Executions.Reconciliation;
using Synentra.Application.Features.Invocations.Admission;
using Synentra.Application.Features.Invocations.Contracts;
using Synentra.Application.Features.Invocations.Evaluation;
using Synentra.Application.Features.Invocations.Normalization;
using Synentra.Application.Features.Invocations.Resume;
using Synentra.Application.Features.Manifests;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Policies.Contracts;
using Synentra.Application.Features.Risk.Contracts;
using Synentra.Application.Features.TaskGrants;
using Synentra.Application.Features.TaskGrants.Contracts;
using Synentra.Application.Features.Trust.Contracts;
using Void = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddSynentraApplication_ShouldRegisterAllCoreServicesAndHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Register external infrastructure ports required by application services
        services.AddScoped(_ => Substitute.For<IDispatcher>());
        services.AddScoped(_ => Substitute.For<IPluginResolver>());
        services.AddScoped(_ => Substitute.For<IAdminPolicy>());
        services.AddScoped(_ => Substitute.For<IManifestProvider>());
        services.AddScoped(_ => Substitute.For<IManifestProviderInternal>());
        services.AddScoped(_ => Substitute.For<IManifestStore>());
        services.AddScoped(_ => Substitute.For<IBundleParser>());
        services.AddScoped(_ => Substitute.For<IRevisionProvider>());
        services.AddScoped(_ => Substitute.For<IOperationRegistry>());
        services.AddScoped(_ => Substitute.For<IRiskProvider>());
        services.AddScoped(_ => Substitute.For<ITrustProvider>());
        services.AddScoped(_ => Substitute.For<IPolicyProvider>());
        services.AddScoped(_ => Substitute.For<IAuditStore>());
        services.AddScoped(_ => Substitute.For<IGrantStore>());
        services.AddScoped(_ => Substitute.For<IApprovalStore>());
        services.AddScoped(_ => Substitute.For<IExecutionClaimStore>());
        services.AddScoped(_ => Substitute.For<IExecutionRecordStore>());
        services.AddScoped(_ => Substitute.For<IInvocationStore>());
        services.AddScoped(_ => Substitute.For<IIdempotencyStore>());
        services.AddScoped(_ => Substitute.For<IOutcomeEvidenceStore>());
        services.AddScoped(_ => Substitute.For<IOutcomeVerifier>());
        services.AddScoped(_ => Substitute.For<IOutbox>());
        services.AddScoped(_ => Substitute.For<IUnitOfWork>());
        services.AddScoped(_ => Substitute.For<IExecutionSnapshotFactory>());

        // Act
        services.AddSynentraApplication();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        // Assert
        sp.GetRequiredService<ApprovalService>().Should().NotBeNull();
        sp.GetRequiredService<TaskGrantService>().Should().NotBeNull();
        sp.GetRequiredService<ManifestManager>().Should().NotBeNull();
        sp.GetRequiredService<IManifestManager>().Should().BeSameAs(sp.GetRequiredService<ManifestManager>());
        sp.GetRequiredService<ResumeService>().Should().NotBeNull();
        sp.GetRequiredService<ReconciliationService>().Should().NotBeNull();
        sp.GetRequiredService<BundleValidator>().Should().NotBeNull();
        sp.GetRequiredService<CanonicalJsonService>().Should().NotBeNull();
        sp.GetRequiredService<AuthorizationService>().Should().NotBeNull();
        sp.GetRequiredService<InvocationNormalizer>().Should().NotBeNull();
        sp.GetRequiredService<DecisionEngine>().Should().NotBeNull();
        sp.GetRequiredService<IInvocationEvaluator>().Should().BeSameAs(sp.GetRequiredService<DecisionEngine>());
        sp.GetRequiredService<AdmissionService>().Should().NotBeNull();
        sp.GetRequiredService<IExecutionAdmission>().Should().BeSameAs(sp.GetRequiredService<AdmissionService>());
        sp.GetRequiredService<IActionHandler<ApproveInvocationCommand, Void>>().Should().BeOfType<ApproveInvocationCommandHandler>();
    }
}
