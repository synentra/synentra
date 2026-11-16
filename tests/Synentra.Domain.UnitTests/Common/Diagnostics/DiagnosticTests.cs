using System.Reflection;
using FluentAssertions;
using Synentra.Domain.Common.Diagnostics;
using Synentra.Domain.Common.Errors;

namespace Synentra.Domain.Tests;

public sealed class DiagnosticTests
{
    [Fact]
    public void Define_ShouldCreateDiagnosticWithExpectedCodeAndProperties()
    {
        var diagnostic = Diagnostic.Define("APR", 7, DiagnosticKind.Conflict, "Already resolved");

        diagnostic.Code.Should().Be("SYN-APR-0007");
        diagnostic.Message.Should().Be("Already resolved");
        diagnostic.Kind.Should().Be(DiagnosticKind.Conflict);
        diagnostic.Sequence.Should().Be((ushort)7);
        diagnostic.ToString().Should().Be("conflict SYN-APR-0007: Already resolved");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Define_ShouldRejectBlankMessage(string? message)
        => FluentActions.Invoking(() => Diagnostic.Define("APR", 1, DiagnosticKind.Validation, message!))
            .Should().Throw<ArgumentException>().WithParameterName("message");

    [Fact]
    public void DomainErrors_ShouldInitializeAllDiagnosticsAndDynamicFactories()
    {
        var nestedTypes = typeof(DomainErrors).GetNestedTypes(BindingFlags.Public);

        foreach (var nestedType in nestedTypes)
        {
            foreach (var field in nestedType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(Diagnostic))
                    field.GetValue(null).Should().BeOfType<Diagnostic>();
            }

            foreach (var method in nestedType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.ReturnType == typeof(Diagnostic) && method.GetParameters().Length == 1)
                    method.Invoke(null, ["test-state"]).Should().BeOfType<Diagnostic>();
            }
        }

        DomainErrors.Approval.CannotApprove("Approved").Code.Should().Be("SYN-APR-0002");
        DomainErrors.Approval.CannotReject("Rejected").Code.Should().Be("SYN-APR-0003");
        DomainErrors.Approval.CannotRevoke("Rejected").Code.Should().Be("SYN-APR-0004");
        DomainErrors.Approval.CannotConsume("Pending").Code.Should().Be("SYN-APR-0005");
        DomainErrors.ExecutionClaim.CannotTransitionToDispatching("Unknown").Code.Should().Be("SYN-EXE-0001");
        DomainErrors.TaskGrant.CannotRevoke("Revoked").Code.Should().Be("SYN-TGR-0001");
    }
}
