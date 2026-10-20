namespace Synentra.Application.Common.Plugins;

public sealed record ConformanceTest(
    string Id, 
    string Description, 
    string AssemblyName, 
    string TypeName);
