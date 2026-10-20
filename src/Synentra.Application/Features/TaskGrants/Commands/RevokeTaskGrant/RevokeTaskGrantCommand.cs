using Synentra.Domain.Security.ValueObjects;
using Synentra.Application.Common.Dispatching;
using Unit = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Features.TaskGrants.Commands.RevokeTaskGrant;

public sealed record RevokeTaskGrantCommand(string GrantId, CanonicalPrincipal By) : ICommand<Unit>;
