using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Components;
using AdvancedPCBuilder.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdvancedPCBuilder.Infrastructure;

/// <summary>
/// Creates the database and seeds 2 tenants with their catalogs and one saved build each
/// (STAGE2_PLAN.md §4; same numbers as Core.Tests/Fixtures/TestComponents.cs).
/// Each tenant is seeded through its OWN tenant-scoped <see cref="AppDbContext"/>, so seeding goes
/// through the same query filters and SaveChanges guard as normal requests (no bypass).
/// Seeded builds are checked by the real resolver and engine, so their stored reports are genuine.
/// </summary>
public static class SeedData
{
    public const string TechShopId = "techshop";
    public const string PcMeistraiId = "pcmeistrai";
    public static readonly Guid TechShopBuildId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid PcMeistraiBuildId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task InitializeAsync(
        DbContextOptions<AppDbContext> options, CompatibilityEngine engine, CancellationToken cancellationToken = default)
    {
        foreach (TenantSeed tenant in Tenants)
        {
            await using var db = new AppDbContext(options, new FixedTenantContext(tenant.Id));
            await db.Database.EnsureCreatedAsync(cancellationToken);   // does nothing if the DB already exists

            if (await db.Tenants.AnyAsync(t => t.Id == tenant.Id, cancellationToken))
            {
                continue;   // already seeded
            }

            db.Tenants.Add(new TenantEntity { Id = tenant.Id, Name = tenant.Name });
            foreach (ComponentSeed component in tenant.Components)
            {
                // No TenantId here: the SaveChanges guard assigns the current tenant.
                db.Components.Add(new ComponentEntity
                {
                    Id = component.Id,
                    Type = ComponentSpecSerializer.TypeOf(component.Spec),
                    Name = component.Name,
                    SpecJson = ComponentSpecSerializer.Serialize(component.Spec)
                });
            }
            await db.SaveChangesAsync(cancellationToken);

            var checker = new CompatibilityCheckService(new BuildResolver(new EfComponentCatalog(db)), engine);
            ValidationResult<CompatibilityReport> check = await checker.CheckAsync(tenant.BuildRequest, cancellationToken);
            if (!check.IsValid)
            {
                throw new InvalidOperationException($"Seed build of tenant '{tenant.Id}' is invalid.");
            }

            var repository = new EfBuildRepository(db);
            await repository.AddAsync(
                new SavedBuild(tenant.BuildId, tenant.BuildName, tenant.BuildRequest, check.Value!, DateTimeOffset.UtcNow),
                cancellationToken);
        }
    }

    // ---- Shared specs (both tenants stock some of the same models under their own IDs) ----

    private static readonly CpuSpec Cpu65Am5 = new("AM5", TdpW: 65);
    private static readonly MotherboardSpec MbAtxAm5 = new(
        FormFactor.Atx, "AM5", MemoryType.Ddr5, DimmSlots: 4, SataPorts: 6,
        M2Slots: [new M2SlotSpec("M2_1", []), new M2SlotSpec("M2_2", [5, 6])]);
    private static readonly MotherboardSpec MbEatxAm5 = new(
        FormFactor.EAtx, "AM5", MemoryType.Ddr5, DimmSlots: 4, SataPorts: 8,
        M2Slots: [new M2SlotSpec("M2_1", []), new M2SlotSpec("M2_2", []), new M2SlotSpec("M2_3", [7, 8])]);
    private static readonly RamSpec RamDdr5_34 = new(MemoryType.Ddr5, HeightMm: 34);
    private static readonly GpuSpec Gpu200_270 = new(LengthMm: 270, BoardPowerW: 200);
    private static readonly AirCoolerSpec CoolAir155 = new(HeightMm: 155, RamClearanceMm: 40, FanCount: 1);
    private static readonly CaseSpec CaseMid360 = new(
        SupportedFormFactors: [FormFactor.Atx, FormFactor.MicroAtx, FormFactor.MiniItx],
        MaxCoolerHeightMm: 165, MaxGpuLengthMm: 360, SupportedFrontRadiatorsMm: [240, 280, 360]);
    private static readonly CaseSpec CaseItx = new(
        SupportedFormFactors: [FormFactor.MiniItx],
        MaxCoolerHeightMm: 160, MaxGpuLengthMm: 330, SupportedFrontRadiatorsMm: []);
    private static readonly PsuSpec Psu600 = new(Wattage: 600);
    private static readonly StorageSpec SsdM2 = new(StorageInterface.M2);

    private static readonly TenantSeed TechShop = new(
        TechShopId,
        "TechShop UAB",
        [
            new("ts-cpu-65-am5", "CPU AM5 65 W", Cpu65Am5),
            new("ts-cpu-120-am5", "CPU AM5 120 W", new CpuSpec("AM5", TdpW: 120)),
            new("ts-cpu-170-am5", "CPU AM5 170 W", new CpuSpec("AM5", TdpW: 170)),
            new("ts-cpu-65-lga1700", "CPU LGA1700 65 W", new CpuSpec("LGA1700", TdpW: 65)),
            new("ts-mb-matx-am5", "Motherboard mATX AM5 DDR5", MbAtxAm5 with { FormFactor = FormFactor.MicroAtx }),
            new("ts-mb-atx-am5", "Motherboard ATX AM5 DDR5", MbAtxAm5),
            new("ts-mb-eatx-am5", "Motherboard E-ATX AM5 DDR5", MbEatxAm5),
            new("ts-ram-ddr5-34", "RAM DDR5 module, 34 mm", RamDdr5_34),
            new("ts-ram-ddr5-56", "RAM DDR5 module, 56 mm (tall heatsink)", new RamSpec(MemoryType.Ddr5, HeightMm: 56)),
            new("ts-ram-ddr4-32", "RAM DDR4 module, 32 mm", new RamSpec(MemoryType.Ddr4, HeightMm: 32)),
            new("ts-gpu-200-270", "GPU 200 W, 270 mm", Gpu200_270),
            new("ts-gpu-220-308", "GPU 220 W, 308 mm", new GpuSpec(LengthMm: 308, BoardPowerW: 220)),
            new("ts-gpu-220-309", "GPU 220 W, 309 mm", new GpuSpec(LengthMm: 309, BoardPowerW: 220)),
            new("ts-gpu-425-300", "GPU 425 W, 300 mm", new GpuSpec(LengthMm: 300, BoardPowerW: 425)),
            new("ts-gpu-250-280", "GPU 250 W, 280 mm", new GpuSpec(LengthMm: 280, BoardPowerW: 250)),
            new("ts-cool-air-155", "Air cooler 155 mm, 1 fan", CoolAir155),
            new("ts-cool-air-155-dual", "Air cooler 155 mm, 2 fans", CoolAir155 with { FanCount = 2 }),
            new("ts-cool-air-160-dual", "Air cooler 160 mm, 2 fans", new AirCoolerSpec(HeightMm: 160, RamClearanceMm: 40, FanCount: 2)),
            new("ts-cool-air-165-tower", "Tower air cooler 165 mm, RAM clearance 32 mm", new AirCoolerSpec(HeightMm: 165, RamClearanceMm: 32, FanCount: 2)),
            new("ts-cool-air-170", "Air cooler 170 mm, 2 fans", new AirCoolerSpec(HeightMm: 170, RamClearanceMm: 40, FanCount: 2)),
            new("ts-cool-aio-240", "AIO 240 mm", new AioCoolerSpec(RadiatorSizeMm: 240, RadiatorThicknessMm: 52, FanCount: 2)),
            new("ts-case-mid-360", "Mid tower case (GPU up to 360 mm)", CaseMid360),
            new("ts-case-itx", "Mini-ITX case", CaseItx),
            new("ts-psu-450", "PSU 450 W", new PsuSpec(Wattage: 450)),
            new("ts-psu-600", "PSU 600 W", Psu600),
            new("ts-psu-750", "PSU 750 W", new PsuSpec(Wattage: 750)),
            new("ts-ssd-m2-1tb", "M.2 NVMe SSD 1 TB", SsdM2),
            new("ts-ssd-sata-1tb", "SATA SSD 1 TB", new StorageSpec(StorageInterface.SataSsd)),
            new("ts-hdd-4tb", "HDD 4 TB", new StorageSpec(StorageInterface.Hdd))
        ],
        TechShopBuildId,
        "Biuro PC",
        // Scenario S1
        new BuildRequest(
            CpuId: "ts-cpu-65-am5", MotherboardId: "ts-mb-matx-am5", Ram: new ComponentQuantity("ts-ram-ddr5-34", 2),
            GpuId: "ts-gpu-200-270", CpuCoolerId: "ts-cool-air-155", CaseId: "ts-case-mid-360", PsuId: "ts-psu-600",
            Storage: [new ComponentQuantity("ts-ssd-m2-1tb", 1)], CaseFans: 3));

    private static readonly TenantSeed PcMeistrai = new(
        PcMeistraiId,
        "PC Meistrai MB",
        [
            new("pm-cpu-65-am5", "CPU AM5 65 W", Cpu65Am5),
            new("pm-mb-atx-am5", "Motherboard ATX AM5 DDR5", MbAtxAm5),
            new("pm-mb-eatx-am5", "Motherboard E-ATX AM5 DDR5", MbEatxAm5),
            new("pm-ram-ddr5-34", "RAM DDR5 module, 34 mm", RamDdr5_34),
            new("pm-gpu-200-270", "GPU 200 W, 270 mm", Gpu200_270),
            new("pm-cool-air-155", "Air cooler 155 mm, 1 fan", CoolAir155),
            new("pm-case-mid-360", "Mid tower case (GPU up to 360 mm)", CaseMid360),
            new("pm-case-itx", "Mini-ITX case", CaseItx),
            new("pm-psu-600", "PSU 600 W", Psu600),
            new("pm-ssd-m2-1tb", "M.2 NVMe SSD 1 TB", SsdM2)
        ],
        PcMeistraiBuildId,
        "Dirbtuvių PC",
        // Scenario S1 with pm- IDs and an ATX board
        new BuildRequest(
            CpuId: "pm-cpu-65-am5", MotherboardId: "pm-mb-atx-am5", Ram: new ComponentQuantity("pm-ram-ddr5-34", 2),
            GpuId: "pm-gpu-200-270", CpuCoolerId: "pm-cool-air-155", CaseId: "pm-case-mid-360", PsuId: "pm-psu-600",
            Storage: [new ComponentQuantity("pm-ssd-m2-1tb", 1)], CaseFans: 3));

    private static readonly TenantSeed[] Tenants = [TechShop, PcMeistrai];

    private sealed record ComponentSeed(string Id, string Name, ComponentSpec Spec);

    private sealed record TenantSeed(
        string Id,
        string Name,
        IReadOnlyList<ComponentSeed> Components,
        Guid BuildId,
        string BuildName,
        BuildRequest BuildRequest);
}
