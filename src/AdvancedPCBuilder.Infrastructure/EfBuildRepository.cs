using System.Text.Json;
using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdvancedPCBuilder.Infrastructure;

/// <summary>
/// <see cref="IBuildRepository"/> on EF Core. Tenant isolation comes from <see cref="AppDbContext"/>:
/// the query filter hides other tenants' builds (so they look "not found"), and the SaveChanges
/// guard assigns the TenantId of new builds.
/// Lookups use FirstOrDefaultAsync, not DbSet.FindAsync, because FindAsync can return an already
/// tracked entity without applying the query filter.
/// </summary>
public sealed class EfBuildRepository : IBuildRepository
{
    private readonly AppDbContext _db;

    public EfBuildRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SavedBuild>> ListAsync(CancellationToken cancellationToken = default)
    {
        List<SavedBuildEntity> entities = await _db.SavedBuilds
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
        return entities.Select(ToModel).ToList();
    }

    public async Task<SavedBuild?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SavedBuildEntity? entity = await _db.SavedBuilds
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task AddAsync(SavedBuild build, CancellationToken cancellationToken = default)
    {
        var entity = new SavedBuildEntity { Id = build.Id };   // TenantId is set by the SaveChanges guard
        CopyToEntity(build, entity);
        _db.SavedBuilds.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(SavedBuild build, CancellationToken cancellationToken = default)
    {
        SavedBuildEntity? entity = await _db.SavedBuilds.FirstOrDefaultAsync(b => b.Id == build.Id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        CopyToEntity(build, entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SavedBuildEntity? entity = await _db.SavedBuilds.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _db.SavedBuilds.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void CopyToEntity(SavedBuild build, SavedBuildEntity entity)
    {
        entity.Name = build.Name;
        entity.RequestJson = JsonSerializer.Serialize(build.Request, JsonDefaults.Options);
        entity.ReportJson = JsonSerializer.Serialize(build.Report, JsonDefaults.Options);
        entity.UpdatedAt = build.UpdatedAt;
    }

    private static SavedBuild ToModel(SavedBuildEntity entity) => new(
        entity.Id,
        entity.Name,
        JsonSerializer.Deserialize<BuildRequest>(entity.RequestJson, JsonDefaults.Options)!,
        JsonSerializer.Deserialize<CompatibilityReport>(entity.ReportJson, JsonDefaults.Options)!,
        entity.UpdatedAt);
}
