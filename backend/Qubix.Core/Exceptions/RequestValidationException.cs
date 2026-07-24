namespace Qubix.Core.Exceptions;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(
        string field,
        params string[] errors)
        : this(new Dictionary<string, string[]>
        {
            [field] = errors
        })
    {
    }

    public RequestValidationException(
        IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            throw new ArgumentException(
                "At least one validation error is required.",
                nameof(errors));
        }

        Errors = errors.ToDictionary(
            pair => pair.Key,
            pair => pair.Value
                .Where(error => !string.IsNullOrWhiteSpace(error))
                .Distinct()
                .ToArray());

        if (Errors.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value.Length == 0))
        {
            throw new ArgumentException(
                "Validation fields and error messages cannot be empty.",
                nameof(errors));
        }
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
