using AdvancedPCBuilder.Core.Application;

namespace AdvancedPCBuilder.Core.Tests.Fixtures;

/// <summary>Build requests with techshop IDs, as a client would send them.</summary>
public static class TestRequests
{
    /// <summary>Scenario S1: Approved, required 414 W.</summary>
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

    /// <summary>Scenario S7: Rejected by R2 and R5, required 600 W.</summary>
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
}
