using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Components;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Fixtures;

/// <summary>
/// Small fluent helper for building a <see cref="BuildSpecification"/> in tests.
/// Starts from the S1 build (valid, Approved), so a test only changes what it is about:
/// <c>TestBuild.ValidBaseline().WithCooler(CoolAir170).Build()</c>.
/// </summary>
public sealed class TestBuild
{
    private BuildSpecification _build;

    private TestBuild(BuildSpecification build)
    {
        _build = build;
    }

    /// <summary>Scenario S1: every rule passes, P = 345 W, required = 414 W.</summary>
    public static TestBuild ValidBaseline() => new(new BuildSpecification(
        Cpu: Cpu65Am5,
        Motherboard: MbMatxAm5,
        Ram: RamDdr5_34,
        RamQuantity: 2,
        Gpu: Gpu200_270,
        Cooler: CoolAir155,
        Case: CaseMid360,
        Psu: Psu600,
        Storage: [new StorageItem(SsdM2, 1)],
        CaseFanCount: 3));

    public TestBuild WithCpu(CpuSpec cpu) => Set(_build with { Cpu = cpu });

    public TestBuild WithMotherboard(MotherboardSpec motherboard) => Set(_build with { Motherboard = motherboard });

    public TestBuild WithRam(RamSpec ram, int quantity) => Set(_build with { Ram = ram, RamQuantity = quantity });

    public TestBuild WithGpu(GpuSpec? gpu) => Set(_build with { Gpu = gpu });

    public TestBuild WithCooler(CoolerSpec? cooler) => Set(_build with { Cooler = cooler });

    public TestBuild WithCase(CaseSpec pcCase) => Set(_build with { Case = pcCase });

    public TestBuild WithPsu(PsuSpec psu) => Set(_build with { Psu = psu });

    /// <summary>Replaces all storage, e.g. <c>.WithStorage((SsdM2, 2), (Hdd, 3))</c>.</summary>
    public TestBuild WithStorage(params (StorageSpec Spec, int Quantity)[] items) =>
        Set(_build with { Storage = items.Select(item => new StorageItem(item.Spec, item.Quantity)).ToList() });

    public TestBuild WithCaseFans(int count) => Set(_build with { CaseFanCount = count });

    public BuildSpecification Build() => _build;

    private TestBuild Set(BuildSpecification build)
    {
        _build = build;
        return this;
    }
}
