namespace AdvancedPCBuilder.Core.Domain.Components;

/// <param name="LengthMm">Card length in millimetres.</param>
/// <param name="BoardPowerW">Total board power (TBP) in watts.</param>
public sealed record GpuSpec(int LengthMm, int BoardPowerW) : ComponentSpec;
