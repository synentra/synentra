using Synentra.Domain.Invocations;
using Synentra.Application.Common.Dispatching;

namespace Synentra.Application.Features.Invocations.Queries.GetInvocation;

public sealed record GetInvocationQuery(string InvocationId) : IQuery<ToolInvocation?>;
