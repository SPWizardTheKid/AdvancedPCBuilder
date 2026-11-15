namespace AdvancedPCBuilder.Core.Domain.Components;

/// <param name="M2Slots">M.2 slots in order (M2_1, M2_2, ...). Drives are assigned to them in this order.</param>
public sealed record MotherboardSpec(
    FormFactor FormFactor,
    string Socket,
    MemoryType MemoryType,
    int DimmSlots,
    int SataPorts,
    IReadOnlyList<M2SlotSpec> M2Slots) : ComponentSpec;
