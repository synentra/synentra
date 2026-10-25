using Microsoft.Extensions.Options;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Intent.Contracts;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Infrastructure.Configuration.Semantic;
using Synentra.Persistence.Features.Security;

namespace Synentra.Infrastructure.Features.Intent;

public sealed class HttpIntentProvider(
    IHttpClientFactory clients, 
    IOptions<SemanticConfiguration> options,
    CredentialBindingStore bindings,
    IRuntimeCredentialResolver credentials) : IIntentProvider
{
    public string ProviderId => "semantic-http";
    public string ContractVersion => "1.0.0";
    public IntentModality SupportedModalities => (IntentModality)(-1);

    public async Task<IntentLabel> ClassifyAsync(IntentEvidence evidence, CancellationToken ct)
    {
        return new("unavailable", 0, IntentStatus.Error, ProviderId);
    }
}
