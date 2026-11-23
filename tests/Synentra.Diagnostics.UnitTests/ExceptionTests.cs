using Synentra.Diagnostics;
using Synentra.Diagnostics.Exceptions;
using FluentAssertions;
using Xunit;

namespace Synentra.Diagnostics.Tests;

public class ExceptionTests
{
    private static Diagnostic Diagnostic => Synentra.Diagnostics.Diagnostic.Define("AUTH", 1, "Failure", DiagnosticCategory.Forbidden);

    [Fact] public void Application_exception_carries_diagnostic_and_inner_exception()
    {
        var inner = new InvalidOperationException("inner"); var sut = new SynentraApplicationException(Diagnostic, inner);
        AssertException(sut, inner);
    }
    [Fact] public void Domain_exception_carries_diagnostic_and_inner_exception()
    { var inner = new InvalidOperationException("inner"); AssertException(new SynentraDomainException(Diagnostic, inner), inner); }
    [Fact] public void Infrastructure_exception_carries_diagnostic_and_inner_exception()
    { var inner = new InvalidOperationException("inner"); AssertException(new SynentraInfrastructureException(Diagnostic, inner), inner); }
    [Fact] public void Authentication_exception_carries_diagnostic_and_inner_exception()
    { var inner = new InvalidOperationException("inner"); AssertException(new AuthenticationApiException(Diagnostic, inner), inner); }
    [Fact] public void Forbidden_exception_carries_diagnostic_and_inner_exception()
    { var inner = new InvalidOperationException("inner"); AssertException(new ForbiddenApiException(Diagnostic, inner), inner); }
    [Fact] public void Idempotency_exception_carries_diagnostic_and_inner_exception()
    { var inner = new InvalidOperationException("inner"); AssertException(new IdempotencyApiException(Diagnostic, inner), inner); }
    [Fact] public void Credential_not_found_exception_carries_diagnostic_and_inner_exception()
    { var inner = new InvalidOperationException("inner"); AssertException(new CredentialNotFoundException(Diagnostic, inner), inner); }

    [Fact]
    public void Exception_without_inner_exception_is_supported()
    { var sut = new SynentraApplicationException(Diagnostic); sut.InnerException.Should().BeNull(); }

    [Fact]
    public void Base_exception_constructor_rejects_null_diagnostic()
        => FluentActions.Invoking(() => new TestSynentraException(null!)).Should().Throw<ArgumentNullException>();

    private static void AssertException(SynentraException sut, Exception inner)
    {
        sut.Diagnostic.Should().Be(Diagnostic);
        sut.Code.Should().Be(Diagnostic.Code);
        sut.Category.Should().Be(Diagnostic.Category);
        sut.Message.Should().Be($"[{Diagnostic.Code}] {Diagnostic.Message}");
        sut.InnerException.Should().BeSameAs(inner);
        sut.ToString().Should().Contain(Diagnostic.ToString());
        sut.ToString().Should().Contain(sut.GetType().FullName!);
    }

    private sealed class TestSynentraException(Diagnostic diagnostic) : SynentraException(diagnostic) { }
}
