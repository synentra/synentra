using System.Text.Json;
using System.Text.Json.Serialization;
using Synentra.Domain.Execution;
using Synentra.Domain.Execution.Evidence;

namespace Synentra.Persistence.Serialization;

internal sealed class OutcomeEvidenceConverter : JsonConverter<OutcomeEvidence>
{
    public override OutcomeEvidence Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var e = doc.RootElement;
        string? S(string name) => e.GetProperty(name).GetString();
        return OutcomeEvidence.Restore(
            S("id")!, 
            S("executionId")!, 
            e.GetProperty("status").Deserialize<OutcomeStatus>(options), 
            S("source"), 
            S("predicateVersion"), 
            S("safeSummary"), 
            S("evidenceDigest"), 
            S("contradictionReason"), 
            e.GetProperty("observedAt").GetDateTimeOffset());
    }

    public override void Write(Utf8JsonWriter writer, OutcomeEvidence value, JsonSerializerOptions options) 
        => JsonSerializer.Serialize(
            writer, 
            new { 
                value.Id, 
                value.ExecutionId, 
                value.Status, 
                value.Source, 
                value.PredicateVersion, 
                value.SafeSummary, 
                value.EvidenceDigest, 
                value.ContradictionReason, 
                value.ObservedAt }, 
            options);
}
