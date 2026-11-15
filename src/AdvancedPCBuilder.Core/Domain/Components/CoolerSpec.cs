namespace AdvancedPCBuilder.Core.Domain.Components;

/// <summary>
/// Base type for CPU coolers. Rules use pattern matching (<c>cooler is AirCoolerSpec air</c>)
/// to apply air-only or AIO-only checks.
/// </summary>
/// <param name="FanCount">Number of fans on the cooler (or on the AIO radiator); each draws power.</param>
public abstract record CoolerSpec(int FanCount) : ComponentSpec;

/// <param name="HeightMm">Total cooler height in millimetres (checked by R2).</param>
/// <param name="RamClearanceMm">Maximum RAM height that fits under the cooler (checked by R7).</param>
public sealed record AirCoolerSpec(int HeightMm, int RamClearanceMm, int FanCount) : CoolerSpec(FanCount);

/// <param name="RadiatorSizeMm">Radiator size, e.g. 240 (checked by R2b).</param>
/// <param name="RadiatorThicknessMm">Radiator + fans thickness; reduces free GPU length (R3).</param>
public sealed record AioCoolerSpec(int RadiatorSizeMm, int RadiatorThicknessMm, int FanCount) : CoolerSpec(FanCount);
