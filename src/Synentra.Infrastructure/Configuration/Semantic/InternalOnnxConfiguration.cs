namespace Synentra.Infrastructure.Configuration.Semantic;

public class InternalOnnxConfiguration
{
    public bool Enabled { get; set; } = true;

    /// <summary>Model distribution type: "Community" (unencrypted ZIP) or "Pro" (AES-256-GCM encrypted ZIP).</summary>
    public string ModelType { get; set; } = "Community";

    /// <summary>Absolute path to the model ZIP package (contains model.onnx, vocabs.txt, labels.json).</summary>
    public string? PackagePath { get; set; }

    /// <summary>Absolute path to the JSON license file that provides the AES-256 decryption key (Pro only).</summary>
    public string? LicensePath { get; set; }

}