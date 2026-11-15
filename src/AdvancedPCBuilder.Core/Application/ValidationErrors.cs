namespace AdvancedPCBuilder.Core.Application;

/// <summary>
/// Collects input validation errors per field (e.g. "cpuId", "ram.quantity", "storage[0].id").
/// The shape matches ASP.NET Core ValidationProblemDetails, so the API can return it as-is.
/// </summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool HasErrors => _errors.Count > 0;

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out List<string>? messages))
        {
            messages = new List<string>();
            _errors[field] = messages;
        }

        messages.Add(message);
    }

    public void AddAll(IReadOnlyDictionary<string, string[]> errors)
    {
        foreach (KeyValuePair<string, string[]> pair in errors)
        {
            foreach (string message in pair.Value)
            {
                Add(pair.Key, message);
            }
        }
    }

    public IReadOnlyDictionary<string, string[]> ToDictionary() =>
        _errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
}
