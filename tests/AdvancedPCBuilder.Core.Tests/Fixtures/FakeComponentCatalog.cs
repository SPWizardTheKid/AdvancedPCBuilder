using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Core.Domain.Components;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Fixtures;

/// <summary>In-memory techshop catalog (ts- IDs from STAGE2_PLAN.md §4) for application tests.</summary>
public sealed class FakeComponentCatalog : IComponentCatalog
{
    private readonly Dictionary<string, CatalogComponent> _components = new[]
    {
        Item("ts-cpu-65-am5", ComponentType.Cpu, Cpu65Am5),
        Item("ts-cpu-170-am5", ComponentType.Cpu, Cpu170Am5),
        Item("ts-mb-matx-am5", ComponentType.Motherboard, MbMatxAm5),
        Item("ts-mb-atx-am5", ComponentType.Motherboard, MbAtxAm5),
        Item("ts-ram-ddr5-34", ComponentType.Ram, RamDdr5_34),
        Item("ts-gpu-200-270", ComponentType.Gpu, Gpu200_270),
        Item("ts-gpu-250-280", ComponentType.Gpu, Gpu250_280),
        Item("ts-cool-air-155", ComponentType.Cooler, CoolAir155),
        Item("ts-cool-air-170", ComponentType.Cooler, CoolAir170),
        Item("ts-cool-aio-240", ComponentType.Cooler, CoolAio240),
        Item("ts-case-mid-360", ComponentType.Case, CaseMid360),
        Item("ts-psu-450", ComponentType.Psu, Psu450),
        Item("ts-psu-600", ComponentType.Psu, Psu600),
        Item("ts-ssd-m2-1tb", ComponentType.Storage, SsdM2),
        Item("ts-hdd-4tb", ComponentType.Storage, Hdd)
    }.ToDictionary(component => component.Id);

    public Task<IReadOnlyList<CatalogComponent>> FindByIdsAsync(
        IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CatalogComponent> found = ids
            .Where(_components.ContainsKey)
            .Select(id => _components[id])
            .ToList();
        return Task.FromResult(found);
    }

    public Task<IReadOnlyList<CatalogComponent>> ListAsync(
        ComponentType? type, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CatalogComponent> list = _components.Values
            .Where(component => type is null || component.Type == type)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<CatalogComponent?> FindAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_components.GetValueOrDefault(id));

    private static CatalogComponent Item(string id, ComponentType type, ComponentSpec spec) => new(id, id, type, spec);
}
