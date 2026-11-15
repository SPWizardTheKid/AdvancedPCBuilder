namespace AdvancedPCBuilder.Core.Domain.Compatibility;

/// <summary>
/// Fixed power estimates for components without a rated power value, plus the PSU margins (spec §3.3).
/// The values come from configuration (appsettings.json section "PowerEstimates"), not from code.
/// They are global, the same for every tenant.
/// </summary>
public sealed record PowerEstimates
{
    public int MotherboardW { get; init; }
    public int RamModuleW { get; init; }
    public int M2SsdW { get; init; }
    public int SataSsdW { get; init; }
    public int HddW { get; init; }

    /// <summary>Per fan, both case fans and cooler fans.</summary>
    public int FanW { get; init; }

    public int AioPumpW { get; init; }

    /// <summary>Required PSU power = total power + this many percent (20 in the spec).</summary>
    public int SafetyMarginPercent { get; init; }

    /// <summary>Warn when the required power uses more than (100 - this) percent of the PSU (10 in the spec).</summary>
    public int WarningHeadroomPercent { get; init; }
}
