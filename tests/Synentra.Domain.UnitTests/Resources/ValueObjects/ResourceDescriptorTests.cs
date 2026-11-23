using FluentAssertions;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Resources.ValueObjects;

namespace Synentra.Domain.Tests;

public sealed class ResourceDescriptorTests
{
    [Fact]
    public void Constructor_ShouldApplyDefaultsAndBuildCanonicalKey()
    {
        var before = DateTimeOffset.UtcNow;
        var descriptor = new ResourceDescriptor("product", "p-123", "tenant-a", "inventory");
        var after = DateTimeOffset.UtcNow;

        descriptor.Type.Should().Be("product");
        descriptor.Id.Should().Be("p-123");
        descriptor.SecurityScope.Should().Be("tenant-a");
        descriptor.AuthorityDomain.Should().Be("inventory");
        descriptor.Version.Should().BeNull();
        descriptor.TrustedAttributes.Should().BeEmpty();
        descriptor.ObservedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        descriptor.CanonicalKey.Should().Be("tenant-a|inventory|product|p-123");
    }

    [Fact]
    public void Constructor_ShouldPreserveOptionalValues()
    {
        var attributes = new Dictionary<string, string> { ["classification"] = "internal" };
        var observedAt = DateTimeOffset.Parse("2026-05-01T09:00:00Z");
        var descriptor = new ResourceDescriptor("document", "doc-1", "scope", "authority", "v3", attributes, observedAt);

        descriptor.Version.Should().Be("v3");
        descriptor.TrustedAttributes.Should().BeSameAs(attributes);
        descriptor.ObservedAt.Should().Be(observedAt);
    }

    [Theory]
    [InlineData(null, "id", "scope", "authority", "SYNENTRA-RSD-0002")]
    [InlineData(" ", "id", "scope", "authority", "SYNENTRA-RSD-0002")]
    [InlineData("type", null, "scope", "authority", "SYNENTRA-RSD-0003")]
    [InlineData("type", " ", "scope", "authority", "SYNENTRA-RSD-0003")]
    [InlineData("type", "id", null, "authority", "SYNENTRA-RSD-0004")]
    [InlineData("type", "id", " ", "authority", "SYNENTRA-RSD-0004")]
    [InlineData("type", "id", "scope", null, "SYNENTRA-RSD-0005")]
    [InlineData("type", "id", "scope", " ", "SYNENTRA-RSD-0005")]
    public void Constructor_ShouldRejectRequiredValues(string? type, string? id, string? scope, string? authority, string code)
        => FluentActions.Invoking(() => new ResourceDescriptor(type!, id!, scope!, authority!))
            .Should().Throw<SynentraDomainException>().Which.Diagnostic.Code.Should().Be(code);

    [Fact]
    public void SameIdentityAs_ShouldCompareCanonicalKeysOrdinally()
    {
        var first = new ResourceDescriptor("product", "P-1", "scope", "authority");
        var same = new ResourceDescriptor("product", "P-1", "scope", "authority", "different-version");
        var different = new ResourceDescriptor("product", "p-1", "scope", "authority");

        first.SameIdentityAs(same).Should().BeTrue();
        first.SameIdentityAs(different).Should().BeFalse();
    }

    [Fact]
    public void ResourceRole_ShouldReportWhetherDescriptorListIsEmpty()
    {
        var empty = new ResourceRole("target", Array.Empty<ResourceDescriptor>());
        var populated = new ResourceRole("subject", [new ResourceDescriptor("user", "u-1", "scope", "iam")]);

        empty.Role.Should().Be("target");
        empty.Descriptors.Should().BeEmpty();
        empty.IsEmpty.Should().BeTrue();
        populated.Role.Should().Be("subject");
        populated.Descriptors.Should().ContainSingle();
        populated.IsEmpty.Should().BeFalse();
    }
}
