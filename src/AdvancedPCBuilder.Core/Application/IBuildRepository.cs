namespace AdvancedPCBuilder.Core.Application;

/// <summary>
/// Saved builds of the CURRENT tenant (port). Another tenant's build behaves exactly like a
/// non-existing one: Find returns null, Update and Delete return false.
/// </summary>
public interface IBuildRepository
{
    Task<IReadOnlyList<SavedBuild>> ListAsync(CancellationToken cancellationToken = default);

    Task<SavedBuild?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(SavedBuild build, CancellationToken cancellationToken = default);

    /// <returns>false when no build with this ID exists for the current tenant.</returns>
    Task<bool> UpdateAsync(SavedBuild build, CancellationToken cancellationToken = default);

    /// <returns>false when no build with this ID exists for the current tenant.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
