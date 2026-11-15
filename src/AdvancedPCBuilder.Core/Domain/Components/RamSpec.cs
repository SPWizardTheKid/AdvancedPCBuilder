namespace AdvancedPCBuilder.Core.Domain.Components;

/// <summary>Specification of ONE RAM module. The quantity is part of the build, not of the spec.</summary>
public sealed record RamSpec(MemoryType MemoryType, int HeightMm) : ComponentSpec;
