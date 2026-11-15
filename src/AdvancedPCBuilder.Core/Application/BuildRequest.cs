namespace AdvancedPCBuilder.Core.Application;

/// <summary>
/// What the user selected: component IDs from the tenant's catalog (spec §3.1 input JSON).
/// Everything is nullable on purpose: missing fields arrive here and are reported by
/// <see cref="BuildResolver"/> as validation errors (HTTP 400), not as JSON parsing failures.
/// Required: CpuId, MotherboardId, Ram, CaseId, PsuId. Optional: GpuId, CpuCoolerId, Storage, CaseFans.
/// </summary>
public sealed record BuildRequest(
    string? CpuId,
    string? MotherboardId,
    ComponentQuantity? Ram,
    string? GpuId,
    string? CpuCoolerId,
    string? CaseId,
    string? PsuId,
    IReadOnlyList<ComponentQuantity?>? Storage,
    int CaseFans = 0);

/// <summary>A component ID with a quantity (used for RAM modules and storage drives).</summary>
public sealed record ComponentQuantity(string? Id, int Quantity);
