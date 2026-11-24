using FluentAssertions;
using Synentra.Application.Common.Versioning;

namespace Synentra.Application.UnitTests.Common.Versioning;

public sealed class CompatibilityPolicyTests
{
    [Fact]
    public void IsSupportedManifestApiVersion_accepts_only_current_version()
    {
        CompatibilityPolicy.IsSupportedManifestApiVersion(CompatibilityPolicy.CurrentManifestApiVersion).Should().BeTrue();
        CompatibilityPolicy.IsSupportedManifestApiVersion(null).Should().BeFalse();
        CompatibilityPolicy.IsSupportedManifestApiVersion("synentra.io/v1").Should().BeFalse();
    }

    [Fact]
    public void IsSupportedPersistenceSchemaVersion_accepts_only_current_version()
    {
        CompatibilityPolicy.IsSupportedPersistenceSchemaVersion(CompatibilityPolicy.CurrentPersistenceSchemaVersion).Should().BeTrue();
        CompatibilityPolicy.IsSupportedPersistenceSchemaVersion(0).Should().BeFalse();
        CompatibilityPolicy.IsSupportedPersistenceSchemaVersion(2).Should().BeFalse();
    }
}
