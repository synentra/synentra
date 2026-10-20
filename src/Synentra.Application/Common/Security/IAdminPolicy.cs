namespace Synentra.Application.Common.Security;

public interface IAdminPolicy
{
    /// <summary>
    /// Whether a reviewer may approve an invocation they themselves requested.
    /// Default (spec): false.
    /// </summary>
    bool AllowsSelfApproval { get; }

    /// <summary>
    /// Whether a reviewer in scope X may approve an invocation whose
    /// requesting agent belongs to scope Y. Default: false (same-scope only).
    /// </summary>
    bool AllowsCrossScopeApproval { get; }

    /// <summary>
    /// Maximum trust score that may be granted via manual adjustment.
    /// Guards against an operator silently elevating an agent past policy.
    /// </summary>
    double MaxManualTrustAdjustment { get; }
}
