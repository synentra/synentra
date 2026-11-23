using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;

namespace Synentra.Domain.Operations.ValueObjects;

public sealed record OperationId
{
    public string Value { get; }

    public OperationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new SynentraDomainException(OperationDiagnostics.InvalidOperationId);

        if (value.Length > 256)
            throw new SynentraDomainException(OperationDiagnostics.OperationIdTooLong);

        Value = value;
    }
}
