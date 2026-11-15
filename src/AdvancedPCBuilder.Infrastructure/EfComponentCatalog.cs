using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Core.Domain.Components;
using AdvancedPCBuilder.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdvancedPCBuilder.Infrastructure;

/// <summary>
/// <see cref="IComponentCatalog"/> on EF Core. There is no tenant code here on purpose:
/// the global query filter in <see cref="AppDbContext"/> limits every query to the current tenant.
/// </summary>
public sealed class EfComponentCatalog : IComponentCatalog
{
    private readonly AppDbContext _db;

    public EfComponentCatalog(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CatalogComponent>> FindByIdsAsync(
        IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default)
    {
        List<ComponentEntity> entities = await _db.Components
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);
        return entities.Select(ToModel).ToList();
    }

    public async Task<IReadOnlyList<CatalogComponent>> ListAsync(
        ComponentType? type, CancellationToken cancellationToken = default)
    {
        IQueryable<ComponentEntity> query = _db.Components.AsNoTracking();
        if (type is not null)
        {
            query = query.Where(c => c.Type == type);
        }

        List<ComponentEntity> entities = await query.OrderBy(c => c.Id).ToListAsync(cancellationToken);
        return entities.Select(ToModel).ToList();
    }

    public async Task<CatalogComponent?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        ComponentEntity? entity = await _db.Components
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    private static CatalogComponent ToModel(ComponentEntity entity) => new(
        entity.Id,
        entity.Name,
        entity.Type,
        ComponentSpecSerializer.Deserialize(entity.Type, entity.SpecJson));
}
