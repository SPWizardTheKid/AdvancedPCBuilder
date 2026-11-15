using AdvancedPCBuilder.Core.Domain.Compatibility;

namespace AdvancedPCBuilder.Core.Application;

/// <summary>
/// Use cases for saved builds. Every save re-runs the compatibility check, so a saved build
/// always carries the report for its current request.
/// </summary>
public sealed class BuildService
{
    public const int MaxNameLength = 100;

    private readonly IBuildRepository _repository;
    private readonly CompatibilityCheckService _checkService;

    public BuildService(IBuildRepository repository, CompatibilityCheckService checkService)
    {
        _repository = repository;
        _checkService = checkService;
    }

    public Task<IReadOnlyList<SavedBuild>> ListAsync(CancellationToken cancellationToken = default) =>
        _repository.ListAsync(cancellationToken);

    public Task<SavedBuild?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.FindAsync(id, cancellationToken);

    public async Task<ValidationResult<SavedBuild>> CreateAsync(
        string? name, BuildRequest? request, CancellationToken cancellationToken = default)
    {
        ValidationResult<CompatibilityReport> check = await ValidateAndCheckAsync(name, request, cancellationToken);
        if (!check.IsValid)
        {
            return ValidationResult<SavedBuild>.Failure(check.Errors);
        }

        var build = new SavedBuild(Guid.NewGuid(), name!.Trim(), request!, check.Value!, DateTimeOffset.UtcNow);
        await _repository.AddAsync(build, cancellationToken);
        return ValidationResult<SavedBuild>.Success(build);
    }

    /// <returns>null when the build does not exist for the current tenant (HTTP 404).</returns>
    public async Task<ValidationResult<SavedBuild>?> UpdateAsync(
        Guid id, string? name, BuildRequest? request, CancellationToken cancellationToken = default)
    {
        // Not found is checked first, so another tenant's ID always gives 404, never 400.
        if (await _repository.FindAsync(id, cancellationToken) is null)
        {
            return null;
        }

        ValidationResult<CompatibilityReport> check = await ValidateAndCheckAsync(name, request, cancellationToken);
        if (!check.IsValid)
        {
            return ValidationResult<SavedBuild>.Failure(check.Errors);
        }

        var build = new SavedBuild(id, name!.Trim(), request!, check.Value!, DateTimeOffset.UtcNow);
        if (!await _repository.UpdateAsync(build, cancellationToken))
        {
            return null;
        }

        return ValidationResult<SavedBuild>.Success(build);
    }

    /// <returns>false when the build does not exist for the current tenant (HTTP 404).</returns>
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);

    /// <summary>Validates the name and the request together, so the user sees all errors at once.</summary>
    private async Task<ValidationResult<CompatibilityReport>> ValidateAndCheckAsync(
        string? name, BuildRequest? request, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("name", "name is required.");
        }
        else if (name.Trim().Length > MaxNameLength)
        {
            errors.Add("name", $"name must be at most {MaxNameLength} characters.");
        }

        if (request is null)
        {
            errors.Add("request", "request is required.");
            return ValidationResult<CompatibilityReport>.Failure(errors.ToDictionary());
        }

        ValidationResult<CompatibilityReport> check = await _checkService.CheckAsync(request, cancellationToken);
        errors.AddAll(check.Errors);

        return errors.HasErrors
            ? ValidationResult<CompatibilityReport>.Failure(errors.ToDictionary())
            : check;
    }
}
