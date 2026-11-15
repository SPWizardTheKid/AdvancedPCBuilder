using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Application;

/// <summary>One component of the current tenant's catalog: its ID, display name, category and spec.</summary>
public sealed record CatalogComponent(string Id, string Name, ComponentType Type, ComponentSpec Spec);
