using System.Text.Json;

namespace Synentra.Application.Features.Manifests.Models;

public sealed class MappingEntryModel
{
    public string? SourcePointer { get; set; }
    public string? TargetPointer { get; set; }
    public string? ResourceField { get; set; }
    public string? Role { get; set; }
    public JsonElement? Constant { get; set; }
}
