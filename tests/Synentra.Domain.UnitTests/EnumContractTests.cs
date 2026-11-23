using FluentAssertions;
using Synentra.Diagnostics;
using Synentra.Domain.Approvals;
using Synentra.Domain.Execution;
using Synentra.Domain.Governance.Decisions;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Obligations;
using Synentra.Domain.Governance.Policies;
using Synentra.Domain.Governance.Risk;
using Synentra.Domain.Governance.Trust;
using Synentra.Domain.Grants;
using Synentra.Domain.Operations;
using Synentra.Domain.Resources;
using Synentra.Domain.Security;

namespace Synentra.Domain.Tests;

public sealed class EnumContractTests
{
    [Fact]
    public void DiagnosticContracts_ShouldExposeAllKindsAndModuleCodes()
    {
        Enum.GetNames<DiagnosticCategory>().Should().Equal(
            "Validation", "Authentication", "Forbidden", "NotFound", "Conflict", "Infrastructure");

        new[]
        {
            DiagnosticModules.Agents, DiagnosticModules.Approvals, DiagnosticModules.Policies,
            DiagnosticModules.Invocations, DiagnosticModules.Executions, DiagnosticModules.Adapters,
            DiagnosticModules.Authentication, DiagnosticModules.Credentials, DiagnosticModules.Governance,
            DiagnosticModules.Infrastructure, DiagnosticModules.Persistence, DiagnosticModules.Plugins,
            DiagnosticModules.TaskGrants, DiagnosticModules.Trust, DiagnosticModules.Risk,
            DiagnosticModules.Operation, DiagnosticModules.Intent, DiagnosticModules.BindingDigest,
            DiagnosticModules.ResourceDescriptor, DiagnosticModules.EffectMeasure, DiagnosticModules.Quarantine,
            DiagnosticModules.Secrets
        }.Should().OnlyContain(module => module.Length >= DiagnosticCode.MinModuleLength && module.Length <= DiagnosticCode.MaxModuleLength && DiagnosticCode.IsValidModule(module));
    }

    [Fact]
    public void ApprovalStatus_ShouldExposeAllPersistedStates()
        => Enum.GetNames<ApprovalStatus>().Should().Equal("Pending", "Approved", "Rejected", "Revoked", "Expired", "Consumed");

    [Fact]
    public void ExecutionEnums_ShouldExposeExpectedStates()
    {
        Enum.GetNames<ClaimState>().Should().Equal("Claimed", "Dispatching", "Responded", "FailedBeforeDispatch", "Unknown", "CancelledBeforeDispatch", "Expired");
        Enum.GetNames<ExecutionStatus>().Should().Equal("NotStarted", "Claimed", "Dispatching", "Responded", "FailedBeforeDispatch", "Unknown", "CancelledBeforeDispatch", "Accepted", "Running", "Completed", "Failed", "CancellationRequested", "Cancelled");
        Enum.GetNames<OutcomeStatus>().Should().Equal("NotChecked", "Pending", "Verified", "Contradicted", "Unknown");
    }

    [Fact]
    public void GovernanceEnums_ShouldExposeExpectedValues()
    {
        Enum.GetNames<DecisionOutcome>().Should().Equal("Allow", "Review", "Deny");
        Enum.GetNames<IntentStatus>().Should().Equal("Available", "Unsupported", "LowConfidence", "Timeout", "Error");
        Enum.GetNames<ObligationPhase>().Should().Equal("BeforeDispatch", "DuringExecution", "BeforeResultRelease", "AfterExecution");
        Enum.GetNames<PolicyOutcome>().Should().Equal("Deny", "Review", "Allow");
        Enum.GetNames<RiskFactorStatus>().Should().Equal("Applied", "Missing", "NotApplicable");
        Enum.GetNames<TrustEvidenceType>().Should().Equal("SuccessfulExecution", "ConfirmedMisuse", "PolicyViolation", "InfrastructureFailure", "ManualAdjustment", "ReviewerAction");
        Enum.GetNames<TrustStatus>().Should().Equal("Unknown", "Normal", "Elevated", "Restricted", "Quarantined");
    }

    [Fact]
    public void IntentModality_ShouldSupportFlagsCombinations()
    {
        (IntentModality.Http | IntentModality.Mcp | IntentModality.Batch).Should().Be((IntentModality)11);
        Enum.GetNames<IntentModality>().Should().Equal("None", "Http", "Mcp", "FunctionCall", "Batch", "Stream");
    }

    [Fact]
    public void OtherEnums_ShouldExposeExpectedValues()
    {
        Enum.GetNames<GrantState>().Should().Equal("Active", "Revoked", "Expired");
        Enum.GetNames<ExecutionModel>().Should().Equal("RequestResponse", "Asynchronous", "Stream", "Batch");
        Enum.GetNames<Mutability>().Should().Equal("ReadOnly", "Mutation", "Mixed");
        Enum.GetNames<ResolutionStatus>().Should().Equal("Available", "NotFound", "Unauthorized", "Unavailable");
        Enum.GetNames<PrincipalType>().Should().Equal("Agent", "Reviewer", "Admin", "TrustedIssuer", "ServiceAccount");
    }
}
