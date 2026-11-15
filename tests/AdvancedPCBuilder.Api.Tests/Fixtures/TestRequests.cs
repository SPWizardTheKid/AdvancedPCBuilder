using AdvancedPCBuilder.Core.Application;

namespace AdvancedPCBuilder.Api.Tests.Fixtures;

/// <summary>
/// Request bodies sent over HTTP, using the seeded catalog IDs (STAGE2_PLAN.md §4).
/// Same values as Core.Tests/Fixtures/TestRequests.cs; test projects do not reference each other.
/// </summary>
public static class TestRequests
{
    /// <summary>Scenario S1 for techshop: Approved, required 414 W.</summary>
    public static readonly BuildRequest S1 = new(
        CpuId: "ts-cpu-65-am5",
        MotherboardId: "ts-mb-matx-am5",
        Ram: new ComponentQuantity("ts-ram-ddr5-34", 2),
        GpuId: "ts-gpu-200-270",
        CpuCoolerId: "ts-cool-air-155",
        CaseId: "ts-case-mid-360",
        PsuId: "ts-psu-600",
        Storage: [new ComponentQuantity("ts-ssd-m2-1tb", 1)],
        CaseFans: 3);

    /// <summary>Scenario S7 for techshop: Rejected by R2 (diff 5) and R5 (diff 150), required 600 W.</summary>
    public static readonly BuildRequest S7 = new(
        CpuId: "ts-cpu-170-am5",
        MotherboardId: "ts-mb-atx-am5",
        Ram: new ComponentQuantity("ts-ram-ddr5-34", 2),
        GpuId: "ts-gpu-250-280",
        CpuCoolerId: "ts-cool-air-170",
        CaseId: "ts-case-mid-360",
        PsuId: "ts-psu-450",
        Storage: [new ComponentQuantity("ts-ssd-m2-1tb", 1)],
        CaseFans: 2);

    /// <summary>S1 with pcmeistrai IDs and an ATX board: a VALID request for pcmeistrai.</summary>
    public static readonly BuildRequest PcMeistraiS1 = new(
        CpuId: "pm-cpu-65-am5",
        MotherboardId: "pm-mb-atx-am5",
        Ram: new ComponentQuantity("pm-ram-ddr5-34", 2),
        GpuId: "pm-gpu-200-270",
        CpuCoolerId: "pm-cool-air-155",
        CaseId: "pm-case-mid-360",
        PsuId: "pm-psu-600",
        Storage: [new ComponentQuantity("pm-ssd-m2-1tb", 1)],
        CaseFans: 3);
}
