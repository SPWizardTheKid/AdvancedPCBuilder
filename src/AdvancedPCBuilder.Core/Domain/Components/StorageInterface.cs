namespace AdvancedPCBuilder.Core.Domain.Components;

/// <summary>How a storage drive is connected. M2 uses an M.2 slot; SataSsd and Hdd use a SATA port.</summary>
public enum StorageInterface
{
    M2,
    SataSsd,
    Hdd
}
