using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Tests.Fixtures;

/// <summary>
/// The "techshop" fixture catalog from STAGE2_PLAN.md §4. Same numbers as the seed data.
/// The comment above each field is the catalog ID.
/// </summary>
public static class TestComponents
{
    /// <summary>Power estimates exactly as in spec §3.3 / appsettings.json.</summary>
    public static readonly PowerEstimates SpecPowerEstimates = new()
    {
        MotherboardW = 50,
        RamModuleW = 5,
        M2SsdW = 8,
        SataSsdW = 5,
        HddW = 10,
        FanW = 3,
        AioPumpW = 5,
        SafetyMarginPercent = 20,
        WarningHeadroomPercent = 10
    };

    // ---- CPU ----
    // ts-cpu-65-am5
    public static readonly CpuSpec Cpu65Am5 = new("AM5", TdpW: 65);
    // ts-cpu-120-am5
    public static readonly CpuSpec Cpu120Am5 = new("AM5", TdpW: 120);
    // ts-cpu-170-am5
    public static readonly CpuSpec Cpu170Am5 = new("AM5", TdpW: 170);
    // ts-cpu-65-lga1700
    public static readonly CpuSpec Cpu65Lga1700 = new("LGA1700", TdpW: 65);

    // ---- Motherboards ----
    // ts-mb-matx-am5
    public static readonly MotherboardSpec MbMatxAm5 = new(
        FormFactor.MicroAtx, "AM5", MemoryType.Ddr5, DimmSlots: 4, SataPorts: 6,
        M2Slots: [new M2SlotSpec("M2_1", []), new M2SlotSpec("M2_2", [5, 6])]);
    // ts-mb-atx-am5
    public static readonly MotherboardSpec MbAtxAm5 = new(
        FormFactor.Atx, "AM5", MemoryType.Ddr5, DimmSlots: 4, SataPorts: 6,
        M2Slots: [new M2SlotSpec("M2_1", []), new M2SlotSpec("M2_2", [5, 6])]);
    // ts-mb-eatx-am5
    public static readonly MotherboardSpec MbEatxAm5 = new(
        FormFactor.EAtx, "AM5", MemoryType.Ddr5, DimmSlots: 4, SataPorts: 8,
        M2Slots: [new M2SlotSpec("M2_1", []), new M2SlotSpec("M2_2", []), new M2SlotSpec("M2_3", [7, 8])]);

    // ---- RAM (one module) ----
    // ts-ram-ddr5-34
    public static readonly RamSpec RamDdr5_34 = new(MemoryType.Ddr5, HeightMm: 34);
    // ts-ram-ddr5-56
    public static readonly RamSpec RamDdr5_56 = new(MemoryType.Ddr5, HeightMm: 56);
    // ts-ram-ddr4-32
    public static readonly RamSpec RamDdr4_32 = new(MemoryType.Ddr4, HeightMm: 32);

    // ---- GPU (name = power W _ length mm) ----
    // ts-gpu-200-270
    public static readonly GpuSpec Gpu200_270 = new(LengthMm: 270, BoardPowerW: 200);
    // ts-gpu-220-308
    public static readonly GpuSpec Gpu220_308 = new(LengthMm: 308, BoardPowerW: 220);
    // ts-gpu-220-309
    public static readonly GpuSpec Gpu220_309 = new(LengthMm: 309, BoardPowerW: 220);
    // ts-gpu-425-300
    public static readonly GpuSpec Gpu425_300 = new(LengthMm: 300, BoardPowerW: 425);
    // ts-gpu-250-280
    public static readonly GpuSpec Gpu250_280 = new(LengthMm: 280, BoardPowerW: 250);

    // ---- CPU coolers ----
    // ts-cool-air-155
    public static readonly AirCoolerSpec CoolAir155 = new(HeightMm: 155, RamClearanceMm: 40, FanCount: 1);
    // ts-cool-air-155-dual
    public static readonly AirCoolerSpec CoolAir155Dual = new(HeightMm: 155, RamClearanceMm: 40, FanCount: 2);
    // ts-cool-air-160-dual
    public static readonly AirCoolerSpec CoolAir160Dual = new(HeightMm: 160, RamClearanceMm: 40, FanCount: 2);
    // ts-cool-air-165-tower
    public static readonly AirCoolerSpec CoolAir165Tower = new(HeightMm: 165, RamClearanceMm: 32, FanCount: 2);
    // ts-cool-air-170
    public static readonly AirCoolerSpec CoolAir170 = new(HeightMm: 170, RamClearanceMm: 40, FanCount: 2);
    // ts-cool-aio-240
    public static readonly AioCoolerSpec CoolAio240 = new(RadiatorSizeMm: 240, RadiatorThicknessMm: 52, FanCount: 2);

    // ---- Cases ----
    // ts-case-mid-360
    public static readonly CaseSpec CaseMid360 = new(
        SupportedFormFactors: [FormFactor.Atx, FormFactor.MicroAtx, FormFactor.MiniItx],
        MaxCoolerHeightMm: 165, MaxGpuLengthMm: 360, SupportedFrontRadiatorsMm: [240, 280, 360]);
    // ts-case-itx
    public static readonly CaseSpec CaseItx = new(
        SupportedFormFactors: [FormFactor.MiniItx],
        MaxCoolerHeightMm: 160, MaxGpuLengthMm: 330, SupportedFrontRadiatorsMm: []);

    // ---- PSU ----
    // ts-psu-450
    public static readonly PsuSpec Psu450 = new(Wattage: 450);
    // ts-psu-600
    public static readonly PsuSpec Psu600 = new(Wattage: 600);
    // ts-psu-750
    public static readonly PsuSpec Psu750 = new(Wattage: 750);

    // ---- Storage ----
    // ts-ssd-m2-1tb
    public static readonly StorageSpec SsdM2 = new(StorageInterface.M2);
    // ts-ssd-sata-1tb
    public static readonly StorageSpec SsdSata = new(StorageInterface.SataSsd);
    // ts-hdd-4tb
    public static readonly StorageSpec Hdd = new(StorageInterface.Hdd);
}
