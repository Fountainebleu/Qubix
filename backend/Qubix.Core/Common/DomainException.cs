namespace Qubix.Core.Common;

public sealed class DomainException(string message) : InvalidOperationException(message);
