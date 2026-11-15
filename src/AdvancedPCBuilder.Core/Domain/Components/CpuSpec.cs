namespace AdvancedPCBuilder.Core.Domain.Components;

/// <param name="Socket">CPU socket, e.g. "AM5" or "LGA1700".</param>
/// <param name="TdpW">Thermal design power in watts; used as the CPU power draw.</param>
public sealed record CpuSpec(string Socket, int TdpW) : ComponentSpec;
