using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Domain.Operations.ValueObjects;

public sealed record OperationId
{
    public string Value { get; }

    public OperationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(DomainErrors.Operation.InvalidOperationId);

        if (value.Length > 256)
            throw new DomainException(DomainErrors.Operation.OperationIdTooLong);

        Value = value;
    }
}
