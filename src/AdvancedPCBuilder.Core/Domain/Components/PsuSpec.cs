namespace AdvancedPCBuilder.Core.Domain.Components;

/// <param name="Wattage">Rated output power in watts.</param>
public sealed record PsuSpec(int Wattage) : ComponentSpec;
