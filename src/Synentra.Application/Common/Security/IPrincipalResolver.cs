using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Application.Common.Security;

public interface IPrincipalResolver
{
    string ResolverId { get; }
    Task<CanonicalPrincipal> ResolveAsync(string credential, CancellationToken ct);
}
