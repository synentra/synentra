using Synentra.Domain.Grants;
using Synentra.Application.Common.Dispatching;

namespace Synentra.Application.Features.TaskGrants.Queries.GetTaskGrant;

public sealed record GetTaskGrantQuery(string GrantId) : IQuery<TaskGrant?>;
