using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Domain.Governance.Intent;

namespace Synentra.Application.Features.Intent.Contracts;

public interface IIntentProvider
{
    string ProviderId { get; }
    string ContractVersion { get; }
    IntentModality SupportedModalities { get; }
    Task<IntentLabel> ClassifyAsync(IntentEvidence evidence, CancellationToken ct);
}
