namespace Synentra.Exceptions;

internal sealed class AuthenticationApiException(string message, Exception? inner = null) : Exception(message, inner);
internal sealed class ForbiddenApiException(string message) : Exception(message);
internal sealed class IdempotencyApiException(string message) : Exception(message);
