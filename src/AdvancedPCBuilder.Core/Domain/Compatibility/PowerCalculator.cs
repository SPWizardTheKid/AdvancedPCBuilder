using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Domain.Compatibility;

/// <summary>
/// The ONLY place where build power is calculated. Used by <see cref="Rules.PsuPowerRule"/> and by
/// <see cref="CompatibilityEngine"/> (for RequiredPsuWatts in the report). Integer arithmetic only.
/// </summary>
public sealed class PowerCalculator
{
    private readonly PowerEstimates _estimates;

    public PowerCalculator(PowerEstimates estimates)
    {
        _estimates = estimates;
    }

    /// <summary>
    /// P = cpu.TdpW + MotherboardW + RamModuleW * ramQty + gpu.BoardPowerW + sum(storage W * qty)
    ///     + FanW * (cooler fans + case fans) + (AIO ? AioPumpW : 0)
    /// </summary>
    public int TotalWatts(BuildSpecification build)
    {
        int total = build.Cpu.TdpW;
        total += _estimates.MotherboardW;
        total += _estimates.RamModuleW * build.RamQuantity;
        total += build.Gpu?.BoardPowerW ?? 0;

        foreach (StorageItem item in build.Storage)
        {
            total += StorageWatts(item.Spec.Interface) * item.Quantity;
        }

        int coolerFans = build.Cooler?.FanCount ?? 0;
        total += _estimates.FanW * (coolerFans + build.CaseFanCount);

        if (build.Cooler is AioCoolerSpec)
        {
            total += _estimates.AioPumpW;
        }

        return total;
    }

    /// <summary>
    /// required = ceil(P * (100 + margin) / 100), written as (P * (100 + margin) + 99) / 100
    /// so it stays in integers. With margin 20: P = 345 -> 414, P = 626 -> 752.
    /// </summary>
    public int RequiredPsuWatts(BuildSpecification build)
    {
        int total = TotalWatts(build);
        return (total * (100 + _estimates.SafetyMarginPercent) + 99) / 100;
    }

    private int StorageWatts(StorageInterface storageInterface) => storageInterface switch
    {
        StorageInterface.M2 => _estimates.M2SsdW,
        StorageInterface.SataSsd => _estimates.SataSsdW,
        StorageInterface.Hdd => _estimates.HddW,
        _ => throw new ArgumentOutOfRangeException(nameof(storageInterface), storageInterface, null)
    };
}
