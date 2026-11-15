using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Infrastructure.Entities;

/// <summary>
/// One catalog component of one tenant. The key is (TenantId, Id): IDs are unique per tenant.
/// The technical values are stored as JSON (see D7 and <see cref="ComponentSpecSerializer"/>).
/// </summary>
public sealed class ComponentEntity : ITenantOwned
{
    public string Id { get; set; } = "";
    public string TenantId { get; set; } = "";
    public ComponentType Type { get; set; }
    public string Name { get; set; } = "";
    public string SpecJson { get; set; } = "";
}
