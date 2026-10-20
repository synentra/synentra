using Synentra.Application.Common.Plugins;

namespace Synentra.Infrastructure.Plugins;

public sealed record PluginRegistration(
    string Id, PluginFamily Family, IReadOnlyList<Type> SupportedInputShapes, 
    IReadOnlyList<Type> SupportedOutputShapes, IReadOnlySet<string> Capabilities) : IPluginRegistration
{
    public string Version => "1.0.0";
    public string ContractVersion => "1.0.0";
    public string ConfigurationJsonSchema => "{\"type\":\"object\"}";
    public SecurityAssumptions SecurityAssumptions => new(true, false, "Trusted configured plugin; host controls registration and secrets.", Array.Empty<string>());
    public IReadOnlyList<ConformanceTest> ConformanceTests => Array.Empty<ConformanceTest>();
}
