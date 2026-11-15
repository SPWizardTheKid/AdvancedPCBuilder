using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdvancedPCBuilder.Infrastructure;

/// <summary>
/// EF Core context for ONE tenant (the one in <see cref="ITenantContext"/>). Tenant isolation is
/// enforced here, in the data layer, by two mechanisms:
/// 1. Global query filters: every query on tenant-owned tables gets "WHERE TenantId = @current".
/// 2. SaveChanges guard: new rows get the current TenantId; changing or deleting a row of
///    another tenant throws <see cref="InvalidOperationException"/>.
/// </summary>
public sealed class AppDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<ComponentEntity> Components => Set<ComponentEntity>();
    public DbSet<SavedBuildEntity> SavedBuilds => Set<SavedBuildEntity>();

    /// <summary>Used by the query filters. EF Core reads it again for every query of this context instance.</summary>
    private string CurrentTenantId => _tenant.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantEntity>(tenant =>
        {
            tenant.ToTable("Tenants");
            tenant.HasKey(t => t.Id);
            tenant.Property(t => t.Name).IsRequired();
        });

        modelBuilder.Entity<ComponentEntity>(component =>
        {
            component.ToTable("Components");
            component.HasKey(c => new { c.TenantId, c.Id });
            component.Property(c => c.Type).HasConversion<string>();
            component.Property(c => c.Name).IsRequired();
            component.Property(c => c.SpecJson).IsRequired();
            component.HasOne<TenantEntity>().WithMany().HasForeignKey(c => c.TenantId);
            component.HasQueryFilter(c => c.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<SavedBuildEntity>(build =>
        {
            build.ToTable("SavedBuilds");
            build.HasKey(b => b.Id);
            build.Property(b => b.Name).IsRequired();
            build.Property(b => b.RequestJson).IsRequired();
            build.Property(b => b.ReportJson).IsRequired();
            // Concurrency token: UPDATE/DELETE SQL also checks "AND TenantId = @original",
            // so even a hand-crafted entity cannot touch another tenant's row.
            build.Property(b => b.TenantId).IsConcurrencyToken();
            build.HasIndex(b => b.TenantId);
            build.HasOne<TenantEntity>().WithMany().HasForeignKey(b => b.TenantId);
            build.HasQueryFilter(b => b.TenantId == CurrentTenantId);
        });
    }

    // Both overloads are overridden; the parameterless SaveChanges()/SaveChangesAsync() call these.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantGuard();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantGuard();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantGuard()
    {
        string currentTenantId = CurrentTenantId;

        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State is EntityState.Unchanged or EntityState.Detached)
            {
                continue;
            }

            if (string.IsNullOrEmpty(currentTenantId))
            {
                throw new InvalidOperationException("Tenant isolation: no current tenant, data cannot be changed.");
            }

            if (entry.State == EntityState.Added)
            {
                // A new row always belongs to the current tenant.
                if (string.IsNullOrEmpty(entry.Entity.TenantId))
                {
                    entry.Entity.TenantId = currentTenantId;
                }
                else if (entry.Entity.TenantId != currentTenantId)
                {
                    throw new InvalidOperationException(
                        $"Tenant isolation: cannot add a row for tenant '{entry.Entity.TenantId}' as tenant '{currentTenantId}'.");
                }
            }
            else
            {
                // Modified or Deleted: both the loaded (original) and the current owner must be the current tenant.
                string? originalTenantId = entry.Property(e => e.TenantId).OriginalValue;
                if (originalTenantId != currentTenantId || entry.Entity.TenantId != currentTenantId)
                {
                    throw new InvalidOperationException(
                        $"Tenant isolation: tenant '{currentTenantId}' cannot change a row of tenant '{originalTenantId}'.");
                }
            }
        }
    }
}
