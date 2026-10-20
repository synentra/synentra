namespace Synentra.Application.Features.Invocations.Exceptions;

public sealed class AuthorityExpiredException(string message) : Exception(message);
