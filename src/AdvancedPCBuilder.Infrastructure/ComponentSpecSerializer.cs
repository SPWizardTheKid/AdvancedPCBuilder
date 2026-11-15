using System.Text.Json;
using System.Text.Json.Nodes;
using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Infrastructure;

/// <summary>
/// Converts a component spec to and from the SpecJson column.
/// The ComponentType column says which spec record to create. Coolers have two shapes
/// (air / AIO), so their JSON also stores "kind": "Air" or "Aio".
/// Example: {"socket":"AM5","tdpW":65}
/// </summary>
public static class ComponentSpecSerializer
{
    private const string CoolerKind = "kind";

    public static ComponentType TypeOf(ComponentSpec spec) => spec switch
    {
        CpuSpec => ComponentType.Cpu,
        MotherboardSpec => ComponentType.Motherboard,
        RamSpec => ComponentType.Ram,
        GpuSpec => ComponentType.Gpu,
        CoolerSpec => ComponentType.Cooler,
        CaseSpec => ComponentType.Case,
        PsuSpec => ComponentType.Psu,
        StorageSpec => ComponentType.Storage,
        _ => throw new ArgumentException($"Unknown spec type {spec.GetType().Name}.", nameof(spec))
    };

    public static string Serialize(ComponentSpec spec)
    {
        // spec.GetType() so the concrete record's properties are written, not only the base type's.
        JsonObject json = JsonSerializer.SerializeToNode(spec, spec.GetType(), JsonDefaults.Options)!.AsObject();

        if (spec is CoolerSpec)
        {
            json[CoolerKind] = spec is AioCoolerSpec ? "Aio" : "Air";
        }

        return json.ToJsonString(JsonDefaults.Options);
    }

    public static ComponentSpec Deserialize(ComponentType type, string json) => type switch
    {
        ComponentType.Cpu => Read<CpuSpec>(json),
        ComponentType.Motherboard => Read<MotherboardSpec>(json),
        ComponentType.Ram => Read<RamSpec>(json),
        ComponentType.Gpu => Read<GpuSpec>(json),
        ComponentType.Cooler => ReadCooler(json),
        ComponentType.Case => Read<CaseSpec>(json),
        ComponentType.Psu => Read<PsuSpec>(json),
        ComponentType.Storage => Read<StorageSpec>(json),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static CoolerSpec ReadCooler(string json)
    {
        string? kind = JsonNode.Parse(json)?[CoolerKind]?.GetValue<string>();
        return kind switch
        {
            "Air" => Read<AirCoolerSpec>(json),
            "Aio" => Read<AioCoolerSpec>(json),
            _ => throw new JsonException($"Unknown cooler kind '{kind}'.")
        };
    }

    private static T Read<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonDefaults.Options)
        ?? throw new JsonException($"Spec JSON for {typeof(T).Name} is empty.");
}
