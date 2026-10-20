namespace Synentra.Application.Features.Manifests.Models;

public sealed class BundleSpec
{
    public List<ProfileModel> Profiles { get; set; } = new();
    public List<TargetModel> Targets { get; set; } = new();
    public List<OperationModel> Operations { get; set; } = new();
    public List<BindingModel> Bindings { get; set; } = new();
    public List<RestrictionModel> Restrictions { get; set; } = new();
    public List<PolicyModel> Policies { get; set; } = new();
}
