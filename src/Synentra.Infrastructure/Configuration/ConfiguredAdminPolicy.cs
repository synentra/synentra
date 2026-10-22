using Microsoft.Extensions.Options;
using Synentra.Application.Common.Security;
using Synentra.Infrastructure.Configuration.Approvals;

namespace Synentra.Infrastructure.Configuration;

public sealed class ConfiguredAdminPolicy(IOptionsMonitor<ApprovalsConfiguration> options) : IAdminPolicy
{
    public bool AllowsSelfApproval => options.CurrentValue.AllowSelfApproval ?? false;
    public bool AllowsCrossScopeApproval => options.CurrentValue.AllowCrossScopeApproval ?? false;
    public double MaxManualTrustAdjustment => options.CurrentValue.MaxManualTrustAdjustment ?? 10;
}
