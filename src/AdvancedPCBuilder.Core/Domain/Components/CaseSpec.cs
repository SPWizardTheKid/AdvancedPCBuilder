namespace AdvancedPCBuilder.Core.Domain.Components;

/// <param name="SupportedFrontRadiatorsMm">Radiator sizes that fit at the front (empty if none).</param>
public sealed record CaseSpec(
    IReadOnlyList<FormFactor> SupportedFormFactors,
    int MaxCoolerHeightMm,
    int MaxGpuLengthMm,
    IReadOnlyList<int> SupportedFrontRadiatorsMm) : ComponentSpec;
