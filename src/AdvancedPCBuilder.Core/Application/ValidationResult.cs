namespace AdvancedPCBuilder.Core.Application;

/// <summary>
/// Either a value (input was valid) or validation errors (input was invalid, HTTP 400).
/// Note: a Rejected build is NOT a validation error; it is a valid result with Status = Rejected.
/// </summary>
public sealed record ValidationResult<T>(T? Value, IReadOnlyDictionary<string, string[]> Errors)
    where T : class
{
    public bool IsValid => Errors.Count == 0;

    public static ValidationResult<T> Success(T value) => new(value, new Dictionary<string, string[]>());

    public static ValidationResult<T> Failure(IReadOnlyDictionary<string, string[]> errors) => new(null, errors);
}
