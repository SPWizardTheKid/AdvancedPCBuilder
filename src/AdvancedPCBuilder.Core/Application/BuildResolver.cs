using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Application;

/// <summary>
/// Validates a <see cref="BuildRequest"/> and maps it to a <see cref="BuildSpecification"/> (spec §3.2 steps 2-3).
/// Checks: required components present, quantities >= 1, case fans 0-10, every ID exists in the
/// current tenant's catalog, and every ID has the right component type.
/// All errors are collected (not only the first one). If there is any error, no spec is returned,
/// so the compatibility rules are never executed on invalid input.
/// </summary>
public sealed class BuildResolver
{
    public const int MaxCaseFans = 10;

    private readonly IComponentCatalog _catalog;

    public BuildResolver(IComponentCatalog catalog)
    {
        _catalog = catalog;
    }

    public async Task<ValidationResult<BuildSpecification>> ResolveAsync(
        BuildRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        IReadOnlyList<ComponentQuantity?> storage = request.Storage ?? [];

        // 1. Shape of the request: required fields and quantities.
        RequireId(errors, "cpuId", request.CpuId);
        RequireId(errors, "motherboardId", request.MotherboardId);
        RequireId(errors, "caseId", request.CaseId);
        RequireId(errors, "psuId", request.PsuId);

        if (request.Ram is null)
        {
            errors.Add("ram", "RAM is required.");
        }
        else
        {
            RequireId(errors, "ram.id", request.Ram.Id);
            RequirePositive(errors, "ram.quantity", request.Ram.Quantity);
        }

        for (int i = 0; i < storage.Count; i++)
        {
            ComponentQuantity? item = storage[i];
            if (item is null)
            {
                errors.Add($"storage[{i}]", "Storage item is required.");
                continue;
            }

            RequireId(errors, $"storage[{i}].id", item.Id);
            RequirePositive(errors, $"storage[{i}].quantity", item.Quantity);
        }

        if (request.CaseFans < 0 || request.CaseFans > MaxCaseFans)
        {
            errors.Add("caseFans", $"Case fans must be between 0 and {MaxCaseFans}.");
        }

        // 2. Load every referenced component of this tenant in one call.
        List<string> ids = new[] { request.CpuId, request.MotherboardId, request.Ram?.Id, request.GpuId,
                                   request.CpuCoolerId, request.CaseId, request.PsuId }
            .Concat(storage.Select(item => item?.Id))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct()
            .ToList();
        Dictionary<string, CatalogComponent> found =
            (await _catalog.FindByIdsAsync(ids, cancellationToken)).ToDictionary(component => component.Id);

        // 3. Every given ID must exist and have the expected type.
        CpuSpec? cpu = Lookup<CpuSpec>(errors, found, "cpuId", request.CpuId, ComponentType.Cpu);
        MotherboardSpec? motherboard = Lookup<MotherboardSpec>(errors, found, "motherboardId", request.MotherboardId, ComponentType.Motherboard);
        RamSpec? ram = Lookup<RamSpec>(errors, found, "ram.id", request.Ram?.Id, ComponentType.Ram);
        GpuSpec? gpu = Lookup<GpuSpec>(errors, found, "gpuId", request.GpuId, ComponentType.Gpu);
        CoolerSpec? cooler = Lookup<CoolerSpec>(errors, found, "cpuCoolerId", request.CpuCoolerId, ComponentType.Cooler);
        CaseSpec? pcCase = Lookup<CaseSpec>(errors, found, "caseId", request.CaseId, ComponentType.Case);
        PsuSpec? psu = Lookup<PsuSpec>(errors, found, "psuId", request.PsuId, ComponentType.Psu);

        var storageItems = new List<StorageItem>();
        for (int i = 0; i < storage.Count; i++)
        {
            StorageSpec? drive = Lookup<StorageSpec>(errors, found, $"storage[{i}].id", storage[i]?.Id, ComponentType.Storage);
            if (drive is not null)
            {
                storageItems.Add(new StorageItem(drive, storage[i]!.Quantity));
            }
        }

        if (errors.HasErrors)
        {
            return ValidationResult<BuildSpecification>.Failure(errors.ToDictionary());
        }

        // No errors means every required spec above was found, so the ! operators are safe.
        return ValidationResult<BuildSpecification>.Success(new BuildSpecification(
            Cpu: cpu!,
            Motherboard: motherboard!,
            Ram: ram!,
            RamQuantity: request.Ram!.Quantity,
            Gpu: gpu,
            Cooler: cooler,
            Case: pcCase!,
            Psu: psu!,
            Storage: storageItems,
            CaseFanCount: request.CaseFans));
    }

    private static void RequireId(ValidationErrors errors, string field, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            errors.Add(field, $"{field} is required.");
        }
    }

    private static void RequirePositive(ValidationErrors errors, string field, int quantity)
    {
        if (quantity < 1)
        {
            errors.Add(field, $"{field} must be at least 1.");
        }
    }

    /// <summary>
    /// Returns the spec for <paramref name="id"/>, or null when the ID is empty (handled by RequireId),
    /// unknown in this tenant's catalog, or of the wrong type (the last two add an error).
    /// </summary>
    private static TSpec? Lookup<TSpec>(
        ValidationErrors errors,
        Dictionary<string, CatalogComponent> found,
        string field,
        string? id,
        ComponentType expectedType)
        where TSpec : ComponentSpec
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (!found.TryGetValue(id, out CatalogComponent? component))
        {
            // Same message whether the ID does not exist at all or belongs to another tenant.
            errors.Add(field, $"Component '{id}' was not found.");
            return null;
        }

        if (component.Spec is not TSpec spec)
        {
            errors.Add(field, $"Component '{id}' is a {component.Type}, expected a {expectedType}.");
            return null;
        }

        return spec;
    }
}
