using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Application;

/// <summary>
/// The component catalog of the CURRENT tenant (port). Implementations must never return
/// another tenant's components: for the caller such an ID simply does not exist.
/// </summary>
public interface IComponentCatalog
{
    /// <summary>Returns the components with the given IDs. Unknown IDs are just missing from the result.</summary>
    Task<IReadOnlyList<CatalogComponent>> FindByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default);

    /// <summary>All components, or only one category when <paramref name="type"/> is given.</summary>
    Task<IReadOnlyList<CatalogComponent>> ListAsync(ComponentType? type, CancellationToken cancellationToken = default);

    Task<CatalogComponent?> FindAsync(string id, CancellationToken cancellationToken = default);
}
