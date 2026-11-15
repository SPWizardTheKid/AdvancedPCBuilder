namespace AdvancedPCBuilder.Infrastructure.Entities;

/// <summary>A tenant (shop). Not tenant-owned itself: the API reads this table to check the X-Tenant-Id header.</summary>
public sealed class TenantEntity
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}
