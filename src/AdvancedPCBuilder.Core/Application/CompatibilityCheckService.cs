using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Compatibility;

namespace AdvancedPCBuilder.Core.Application;

/// <summary>
/// Use case "check a build": validate and resolve the request, then run the engine.
/// Invalid input → validation errors (rules are not executed). Valid input → a report,
/// which may itself be Rejected (that is a business result, not an error).
/// </summary>
public sealed class CompatibilityCheckService
{
    private readonly BuildResolver _resolver;
    private readonly CompatibilityEngine _engine;

    public CompatibilityCheckService(BuildResolver resolver, CompatibilityEngine engine)
    {
        _resolver = resolver;
        _engine = engine;
    }

    public async Task<ValidationResult<CompatibilityReport>> CheckAsync(
        BuildRequest request, CancellationToken cancellationToken = default)
    {
        ValidationResult<BuildSpecification> resolved = await _resolver.ResolveAsync(request, cancellationToken);
        if (!resolved.IsValid)
        {
            return ValidationResult<CompatibilityReport>.Failure(resolved.Errors);
        }

        CompatibilityReport report = _engine.Check(resolved.Value!);
        return ValidationResult<CompatibilityReport>.Success(report);
    }
}
