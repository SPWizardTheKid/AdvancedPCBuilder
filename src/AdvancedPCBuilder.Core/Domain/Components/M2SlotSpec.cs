namespace AdvancedPCBuilder.Core.Domain.Components;

/// <param name="Name">Slot name, e.g. "M2_2".</param>
/// <param name="SharedSataPorts">SATA port numbers disabled when this slot is occupied (empty if none).</param>
public sealed record M2SlotSpec(string Name, IReadOnlyList<int> SharedSataPorts);
