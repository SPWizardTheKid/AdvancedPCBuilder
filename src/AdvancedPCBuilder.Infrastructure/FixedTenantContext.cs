using AdvancedPCBuilder.Core.Application;

namespace AdvancedPCBuilder.Infrastructure;

/// <summary>
/// A tenant context with a fixed tenant ID. Used outside HTTP requests: seeding each tenant
/// through its own <see cref="AppDbContext"/>, and tests.
/// </summary>
public sealed class FixedTenantContext : ITenantContext
{
    public FixedTenantContext(string tenantId)
    {
        TenantId = tenantId;
    }

    public string TenantId { get; }
}
