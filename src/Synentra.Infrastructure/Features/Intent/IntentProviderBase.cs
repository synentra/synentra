using System.Text.Json;
using System.Text;
using Synentra.Application.Features.Intent.Contracts;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Intent.ValueObjects;

namespace Synentra.Infrastructure.Features.Intent;

public abstract class IntentProviderBase : IIntentProvider
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public abstract string ProviderId { get; }
    public virtual string ContractVersion => "1.0.0";
    public abstract IntentModality SupportedModalities { get; }

    public abstract Task<IntentLabel> ClassifyAsync(IntentEvidence evidence, CancellationToken ct);

    protected IntentLabel Fallback(IntentStatus status = IntentStatus.Error) => new("suspicious", 0, status, ProviderId);

    protected IntentLabel Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Fallback();

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var value = root.TryGetProperty("intent", out var intent) ? intent.GetString() : null;
            var confidence = root.TryGetProperty("confidence", out var score) && score.TryGetDouble(out var parsed)
                ? Math.Clamp(parsed, 0, 1)
                : 0;

            if (string.IsNullOrWhiteSpace(value))
                return Fallback();

            return new IntentLabel(value, confidence, IntentStatus.Available, ProviderId);
        }
        catch (JsonException)
        {
            return Fallback();
        }
    }

    protected static string BuildPrompt(IntentEvidence evidence) => JsonSerializer.Serialize(new
    {
        operationId = evidence.OperationId,
        family = evidence.OperationFamily,
        action = evidence.Action,
        arguments = evidence.Arguments,
        context = evidence.Context,
        declaredPurpose = evidence.DeclaredPurpose
    }, JsonOptions);

    protected static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string endpoint, object payload, CancellationToken ct) 
        => client.PostAsync(endpoint, new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json"), ct);

    protected static readonly string Prompt = "Classify the request intent. Return only JSON: {\"intent\":\"<label>\",\"confidence\":<0.0-1.0>}. Use labels authenticate, authorize, safe_read, list, export, safe_write, update, create, destructive_delete, soft_delete, admin_action, configure, bulk_export, bulk_import, harmful, suspicious, escalate_privileges, audit, compliance_check, health_check, maintenance.";
}
