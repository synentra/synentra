using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;

namespace Synentra.Domain.Invocations.ValueObjects;

public sealed record BindingDigest
{
    public string Value { get; }

    public BindingDigest(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64)
            throw new SynentraDomainException(BindingDigestDiagnostics.InvalidBindingDigest);

        Value = value.ToLowerInvariant();
    }
}
