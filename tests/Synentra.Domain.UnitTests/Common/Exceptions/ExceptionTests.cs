using FluentAssertions;
using Synentra.Domain.Common.Diagnostics;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Domain.Tests;

public sealed class ExceptionTests
{
    [Fact]
    public void DomainException_ShouldExposeDiagnosticMessageAndInnerException()
    {
        var diagnostic = Diagnostic.Define("ERR", 1, "Invalid value", DiagnosticCategory.Validation);
        var inner = new InvalidOperationException("inner");
        var exception = new DomainException(diagnostic, inner);

        exception.Diagnostic.Should().Be(diagnostic);
        exception.Message.Should().Be("Invalid value");
        exception.InnerException.Should().BeSameAs(inner);
        exception.ToString().Should().Contain("validation SYNENTRA-ERR-0001: Invalid value");
        exception.ToString().Should().Contain(nameof(DomainException));
    }

    [Fact]
    public void DomainException_ShouldAllowMissingInnerException()
    {
        var diagnostic = Diagnostic.Define("ERR", 2, "Failure", DiagnosticCategory.Infrastructure);
        var exception = new DomainException(diagnostic);
        exception.InnerException.Should().BeNull();
        exception.Diagnostic.Should().Be(diagnostic);
    }

    [Fact]
    public void SynentraException_ShouldRejectNullDiagnostic()
        => FluentActions.Invoking(() => new DomainException(null!))
            .Should().Throw<ArgumentNullException>().WithParameterName("diagnostic");
}
