using FluentAssertions;
using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Invocations.ValueObjects;
using Synentra.Domain.Operations.ValueObjects;

namespace Synentra.Domain.Tests;

public sealed class InvocationValueObjectTests
{
    [Fact]
    public void InvocationId_ShouldCreateNewAndEmptyIdsAndFormatValue()
    {
        var first = InvocationId.New();
        var second = InvocationId.New();
        first.Value.Should().NotBe(Guid.Empty);
        second.Value.Should().NotBe(Guid.Empty);
        first.Should().NotBe(second);
        InvocationId.Empty.Value.Should().Be(Guid.Empty);
        var explicitId = new InvocationId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        explicitId.ToString().Should().Be("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("short")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void BindingDigest_ShouldRejectBlankOrNon64CharacterValues(string? value)
        => FluentActions.Invoking(() => new BindingDigest(value!))
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(BindingDigestDiagnostics.InvalidBindingDigest);

    [Fact]
    public void BindingDigest_ShouldNormalizeValueToLowercase()
    {
        var value = new string('A', 64);
        new BindingDigest(value).Value.Should().Be(new string('a', 64));
        // The domain object validates length, not hexadecimal syntax.
        new BindingDigest(new string('z', 64)).Value.Should().Be(new string('z', 64));
    }

    [Fact]
    public void OperationId_ShouldPreserveValidValueAndAllowMaximumLength()
    {
        new OperationId("inventory.product.get").Value.Should().Be("inventory.product.get");
        new OperationId(new string('x', 256)).Value.Should().HaveLength(256);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void OperationId_ShouldRejectMissingValue(string? value)
        => FluentActions.Invoking(() => new OperationId(value!))
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(OperationDiagnostics.InvalidOperationId);

    [Fact]
    public void OperationId_ShouldRejectValuesLongerThan256Characters()
        => FluentActions.Invoking(() => new OperationId(new string('x', 257)))
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(OperationDiagnostics.OperationIdTooLong);
}
