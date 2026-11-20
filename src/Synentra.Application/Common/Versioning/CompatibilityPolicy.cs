namespace Synentra.Application.Common.Versioning;

public static class CompatibilityPolicy
{
    public const string CurrentApiVersion = "v2";
    public const string CurrentManifestApiVersion = "synentra.io/v2alpha2";
    public const int CurrentPersistenceSchemaVersion = 1;

    public static bool IsSupportedManifestApiVersion(string? version)
        => string.Equals(version, CurrentManifestApiVersion, StringComparison.Ordinal);

    public static bool IsSupportedPersistenceSchemaVersion(int version)
        => version == CurrentPersistenceSchemaVersion;
}
