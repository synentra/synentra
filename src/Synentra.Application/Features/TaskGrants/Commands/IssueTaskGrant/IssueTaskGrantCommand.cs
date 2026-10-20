using Synentra.Domain.Grants;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Application.Common.Dispatching;

namespace Synentra.Application.Features.TaskGrants.Commands.IssueTaskGrant;

public sealed record IssueTaskGrantCommand(
    CanonicalPrincipal Issuer, 
    TaskGrant Requested, 
    IReadOnlyDictionary<string, long> IssuerCeilings) : ICommand<TaskGrant>;
