namespace Synentra.Infrastructure.Configuration.Approvals;

public class ApprovalsConfiguration: ISynentraConfiguration
{
    public static string SectionName => "Approvals";

    public bool? AllowSelfApproval { get; set; } = false;
    public bool? AllowCrossScopeApproval { get; set; } = false;
    public int? MaxManualTrustAdjustment { get; set; } = 10;
}
