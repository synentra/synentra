using FluentAssertions;
using Synentra.Diagnostics;
using Synentra.Domain.Common.Results;

namespace Synentra.Domain.Tests;

public sealed class ResultTests
{
    private static readonly Diagnostic Error = Diagnostic.Define("TST", 1, "Test failure", DiagnosticCategory.Validation);

    [Fact]
    public void NonGenericResult_ShouldRepresentSuccessAndFailure()
    {
        var success = Result.Success();
        success.IsSuccess.Should().BeTrue();
        success.Diagnostic.Should().BeNull();

        var failure = Result.Failure(Error);
        failure.IsSuccess.Should().BeFalse();
        failure.Diagnostic.Should().Be(Error);
    }

    [Fact]
    public async Task NonGenericResult_ShouldSupportAsyncFactories()
    {
        var success = await Result.SuccessAsync();
        var failure = await Result.FailureAsync(Error);
        success.IsSuccess.Should().BeTrue();
        failure.IsSuccess.Should().BeFalse();
        failure.Diagnostic.Should().Be(Error);
    }

    [Fact]
    public void GenericResult_ShouldRepresentSuccessAndFailure()
    {
        var success = Result<string>.Success("value");
        success.IsSuccess.Should().BeTrue();
        success.Value.Should().Be("value");
        success.Diagnostic.Should().BeNull();

        var failure = Result<string>.Failure(Error);
        failure.IsSuccess.Should().BeFalse();
        failure.Value.Should().BeNull();
        failure.Diagnostic.Should().Be(Error);
    }

    [Fact]
    public async Task GenericResult_ShouldSupportAsyncFactories()
    {
        var success = await Result<int>.SuccessAsync(42);
        var failure = await Result<int>.FailureAsync(Error);
        success.IsSuccess.Should().BeTrue();
        success.Value.Should().Be(42);
        failure.IsSuccess.Should().BeFalse();
        failure.Diagnostic.Should().Be(Error);
    }

    [Fact]
    public void GenericResult_ShouldSupportImplicitConversions()
    {
        Result<int> fromValue = 17;
        Result<int> fromDiagnostic = Error;
        fromValue.IsSuccess.Should().BeTrue();
        fromValue.Value.Should().Be(17);
        fromDiagnostic.IsSuccess.Should().BeFalse();
        fromDiagnostic.Diagnostic.Should().Be(Error);
    }
}
