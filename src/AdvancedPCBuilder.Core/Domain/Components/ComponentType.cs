namespace AdvancedPCBuilder.Core.Domain.Components;

/// <summary>
/// Catalog category of a component. One value per component field of a build request
/// (Cooler covers both air coolers and AIOs).
/// </summary>
public enum ComponentType
{
    Cpu,
    Motherboard,
    Ram,
    Gpu,
    Cooler,
    Case,
    Psu,
    Storage
}
