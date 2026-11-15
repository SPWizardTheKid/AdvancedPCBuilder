using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Api.Contracts;

/// <summary>
/// One catalog component as JSON. <c>Specs</c> is typed as object on purpose: the JSON serializer
/// then writes the real record (e.g. CpuSpec → { "socket": "AM5", "tdpW": 65 }) instead of the
/// empty abstract base type.
/// </summary>
public sealed record ComponentResponse(string Id, string Name, ComponentType Type, object Specs)
{
    public static ComponentResponse From(CatalogComponent component) => new(
        component.Id,
        component.Name,
        component.Type,
        component.Spec);
}
