using Synentra.Domain.Grants;
using Synentra.Application.Common.Dispatching;

namespace Synentra.Application.Features.TaskGrants.Queries.ListActiveTaskGrants;

public sealed record ListActiveTaskGrantsQuery(string AgentId, string SecurityScope) : IQuery<IReadOnlyList<TaskGrant>>;
