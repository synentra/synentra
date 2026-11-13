namespace Synentra.Infrastructure.Intent.Providers.InternalBert;

public sealed record ModelAssets(
    ReadOnlyMemory<byte> OnnxBytes,
    string[] VocabLines,
    string[] IntentLabels);
