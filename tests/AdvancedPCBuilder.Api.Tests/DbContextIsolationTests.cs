using AdvancedPCBuilder.Core.Domain.Components;
using AdvancedPCBuilder.Infrastructure;
using AdvancedPCBuilder.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdvancedPCBuilder.Api.Tests;

/// <summary>
/// Tenant isolation in the data layer itself, without HTTP: the global query filters and the
/// SaveChanges guard of <see cref="AppDbContext"/>. These tests use the DbContext directly, the way
/// a buggy repository or a forgotten WHERE clause would, and show that it still cannot leak or
/// change another tenant's rows.
/// </summary>
public class DbContextIsolationTests : IClassFixture<ApiFactory>
{
    private const string TechShop = ApiFactory.TechShop;
    private const string PcMeistrai = ApiFactory.PcMeistrai;

    private readonly ApiFactory _factory;

    public DbContextIsolationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // ---- Query filters ----

    [Fact]
    public async Task TechShopContext_SeesOnlyTechShopRows()
    {
        await using AppDbContext db = _factory.CreateDbContext(TechShop);

        List<ComponentEntity> components = await db.Components.ToListAsync();
        List<SavedBuildEntity> builds = await db.SavedBuilds.ToListAsync();

        Assert.Equal(29, components.Count);
        Assert.All(components, c => Assert.Equal(TechShop, c.TenantId));
        Assert.Equal(SeedData.TechShopBuildId, Assert.Single(builds).Id);
    }

    [Fact]
    public async Task PcMeistraiContext_SeesOnlyPcMeistraiRows()
    {
        await using AppDbContext db = _factory.CreateDbContext(PcMeistrai);

        List<ComponentEntity> components = await db.Components.ToListAsync();
        List<SavedBuildEntity> builds = await db.SavedBuilds.ToListAsync();

        Assert.Equal(10, components.Count);
        Assert.All(components, c => Assert.Equal(PcMeistrai, c.TenantId));
        Assert.Equal(SeedData.PcMeistraiBuildId, Assert.Single(builds).Id);
    }

    [Fact]
    public async Task TechShopContext_LookupOfPcMeistraiBuildById_ReturnsNull()
    {
        await using AppDbContext db = _factory.CreateDbContext(TechShop);

        SavedBuildEntity? build = await db.SavedBuilds.FirstOrDefaultAsync(b => b.Id == SeedData.PcMeistraiBuildId);

        Assert.Null(build);
    }

    // ---- SaveChanges guard ----

    [Fact]
    public async Task ModifyingPcMeistraiRow_ThroughTechShopContext_Throws_AndRowIsUnchanged()
    {
        SavedBuildEntity foreignBuild = await LoadPcMeistraiBuildAsync();

        await using (AppDbContext techshopDb = _factory.CreateDbContext(TechShop))
        {
            // Attach skips the query filter: this is the "bypass" the guard must stop.
            techshopDb.SavedBuilds.Attach(foreignBuild);
            foreignBuild.Name = "Hacked";

            await Assert.ThrowsAsync<InvalidOperationException>(() => techshopDb.SaveChangesAsync());
        }

        Assert.Equal("Dirbtuvių PC", (await LoadPcMeistraiBuildAsync()).Name);
    }

    [Fact]
    public async Task DeletingPcMeistraiRow_ThroughTechShopContext_Throws_AndRowStillExists()
    {
        SavedBuildEntity foreignBuild = await LoadPcMeistraiBuildAsync();

        await using (AppDbContext techshopDb = _factory.CreateDbContext(TechShop))
        {
            techshopDb.SavedBuilds.Remove(foreignBuild);   // Remove attaches it in the Deleted state

            await Assert.ThrowsAsync<InvalidOperationException>(() => techshopDb.SaveChangesAsync());
        }

        Assert.Equal(SeedData.PcMeistraiBuildId, (await LoadPcMeistraiBuildAsync()).Id);
    }

    [Fact]
    public async Task AddingRowForPcMeistrai_ThroughTechShopContext_Throws()
    {
        await using AppDbContext techshopDb = _factory.CreateDbContext(TechShop);
        techshopDb.Components.Add(new ComponentEntity
        {
            Id = "pm-planted-cpu",
            TenantId = PcMeistrai,
            Type = ComponentType.Cpu,
            Name = "Planted CPU",
            SpecJson = """{"socket":"AM5","tdpW":65}"""
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => techshopDb.SaveChangesAsync());
    }

    [Fact]
    public async Task MovingOwnRowToPcMeistrai_Throws()
    {
        await using AppDbContext techshopDb = _factory.CreateDbContext(TechShop);
        SavedBuildEntity ownBuild = await techshopDb.SavedBuilds.SingleAsync();

        ownBuild.TenantId = PcMeistrai;

        await Assert.ThrowsAsync<InvalidOperationException>(() => techshopDb.SaveChangesAsync());
    }

    /// <summary>Reads pcmeistrai's seeded build through pcmeistrai's own context (not tracked).</summary>
    private async Task<SavedBuildEntity> LoadPcMeistraiBuildAsync()
    {
        await using AppDbContext db = _factory.CreateDbContext(PcMeistrai);
        return await db.SavedBuilds.AsNoTracking().SingleAsync();
    }
}
