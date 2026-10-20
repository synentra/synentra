using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Invocations.Contracts;

namespace Synentra.Application.Features.Invocations.Queries.GetInvocationResult;

public sealed record GetInvocationResultQuery(string InvocationId) : IQuery<InvocationResultDto?>;
