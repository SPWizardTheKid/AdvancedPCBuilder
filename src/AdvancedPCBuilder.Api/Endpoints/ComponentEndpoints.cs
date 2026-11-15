using AdvancedPCBuilder.Api.Contracts;
using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Core.Domain.Components;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AdvancedPCBuilder.Api.Endpoints;

/// <summary>GET /api/components — the current tenant's catalog (other tenants' components are invisible).</summary>
public static class ComponentEndpoints
{
    public static void MapComponentEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/components").WithTags("Components");
        group.MapGet("/", ListAsync);
        group.MapGet("/{id}", GetAsync);
    }

    /// <summary>All components, or one category with ?type=Cpu (also Motherboard, Ram, Gpu, Cooler, Case, Psu, Storage).</summary>
    private static async Task<Results<Ok<List<ComponentResponse>>, ValidationProblem>> ListAsync(
        string? type, IComponentCatalog catalog, CancellationToken cancellationToken)
    {
        ComponentType? componentType = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            // Enum.IsDefined rejects numbers like "42", which Enum.TryParse would accept.
            if (!Enum.TryParse(type, ignoreCase: true, out ComponentType parsed) || !Enum.IsDefined(parsed))
            {
                string allowed = string.Join(", ", Enum.GetNames<ComponentType>());
                return ApiProblems.Validation(new Dictionary<string, string[]>
                {
                    ["type"] = [$"Unknown component type '{type}'. Allowed: {allowed}."]
                });
            }

            componentType = parsed;
        }

        IReadOnlyList<CatalogComponent> components = await catalog.ListAsync(componentType, cancellationToken);
        return TypedResults.Ok(components.Select(ComponentResponse.From).ToList());
    }

    private static async Task<Results<Ok<ComponentResponse>, ProblemHttpResult>> GetAsync(
        string id, IComponentCatalog catalog, CancellationToken cancellationToken)
    {
        CatalogComponent? component = await catalog.FindAsync(id, cancellationToken);
        if (component is null)
        {
            return ApiProblems.NotFound("Component", id);
        }

        return TypedResults.Ok(ComponentResponse.From(component));
    }
}
