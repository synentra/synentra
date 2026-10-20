namespace Synentra.Application.Common.Plugins;

public interface IPluginRegistration
{
    string Id { get; }
    string Version { get; }
    string ContractVersion { get; }
    string ConfigurationJsonSchema { get; }
    IReadOnlySet<string> Capabilities { get; }
    IReadOnlyList<Type> SupportedInputShapes { get; }
    IReadOnlyList<Type> SupportedOutputShapes { get; }
    SecurityAssumptions SecurityAssumptions { get; }
    IReadOnlyList<ConformanceTest> ConformanceTests { get; }
    PluginFamily Family { get; }
}
