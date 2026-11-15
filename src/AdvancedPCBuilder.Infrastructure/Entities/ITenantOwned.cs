namespace AdvancedPCBuilder.Infrastructure.Entities;

/// <summary>
/// Marks a row that belongs to one tenant. <see cref="AppDbContext"/> filters every query on it
/// and its SaveChanges guard blocks writes to rows of other tenants.
/// </summary>
public interface ITenantOwned
{
    string TenantId { get; set; }
}
