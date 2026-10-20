using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Domain.Invocations.ValueObjects;

public sealed record BindingDigest
{
    public string Value { get; }

    public BindingDigest(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64)
            throw new DomainException(DomainErrors.BindingDigest.InvalidBindingDigest);

        Value = value.ToLowerInvariant();
    }
}
