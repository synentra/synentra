using Microsoft.Extensions.Options;
using Synentra.Application.Common.Security;
using Synentra.Infrastructure.Configuration.Approvals;
using Synentra.Infrastructure.Configuration.Trust;

namespace Synentra.Infrastructure.Configuration;

public sealed class ConfiguredAdminPolicy(
    IOptionsMonitor<ApprovalsConfiguration> options, 
    IOptionsMonitor<TrustConfiguration> trustOptions) : IAdminPolicy
{
    public bool AllowsSelfApproval => options.CurrentValue.AllowSelfApproval ?? false;
    public bool AllowsCrossScopeApproval => options.CurrentValue.AllowCrossScopeApproval ?? false;
    public double MaxManualTrustAdjustment => trustOptions.CurrentValue.MaxManualAdjustment ?? 10;
}
