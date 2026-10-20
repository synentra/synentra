namespace Synentra.Application.Features.Executions.Contracts;

public sealed record TransportReceipt(
    ReceiptStatus Status, 
    string? UpstreamReference, 
    string? Payload, 
    int? StatusCode, 
    DateTimeOffset DispatchTimestamp, 
    string? ErrorMessage = null)
{
    public static TransportReceipt Unknown(string reason) 
        => new(ReceiptStatus.Unknown, null, null, null, DateTimeOffset.UtcNow, reason);
}
