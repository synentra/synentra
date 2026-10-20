using Microsoft.Extensions.Options;
using Synentra.Application.Common.Security;

namespace Synentra.Infrastructure.Configuration;

public sealed class ConfiguredAdminPolicy(IOptionsMonitor<InfrastructureOptions> options) : IAdminPolicy
{
    public bool AllowsSelfApproval => options.CurrentValue.AllowSelfApproval;
    public bool AllowsCrossScopeApproval => options.CurrentValue.AllowCrossScopeApproval;
    public double MaxManualTrustAdjustment => options.CurrentValue.MaxManualTrustAdjustment;
}
