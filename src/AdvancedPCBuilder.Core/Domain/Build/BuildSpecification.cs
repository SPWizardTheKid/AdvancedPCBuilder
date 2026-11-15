using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Domain.Build;

/// <summary>
/// A fully resolved build: every selected component's spec plus quantities.
/// This is the only input of the compatibility engine. Input validation (required parts,
/// quantities >= 1, known IDs) happens before this object is created.
/// </summary>
/// <param name="Gpu">null when no discrete GPU is selected; GPU rules are then skipped.</param>
/// <param name="Cooler">null when no CPU cooler is selected; cooler rules are then skipped.</param>
public sealed record BuildSpecification(
    CpuSpec Cpu,
    MotherboardSpec Motherboard,
    RamSpec Ram,
    int RamQuantity,
    GpuSpec? Gpu,
    CoolerSpec? Cooler,
    CaseSpec Case,
    PsuSpec Psu,
    IReadOnlyList<StorageItem> Storage,
    int CaseFanCount);
