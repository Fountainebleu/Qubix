namespace Qubix.Core.Exceptions;

public sealed class AuthenticationRequiredException(string message) : Exception(message);
