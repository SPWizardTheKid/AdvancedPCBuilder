using AdvancedPCBuilder.Core.Application;

namespace AdvancedPCBuilder.Api.Tenancy;

/// <summary>
/// The tenant of the current HTTP request. Registered as scoped (one instance per request).
/// <see cref="TenantResolutionMiddleware"/> sets it from the X-Tenant-Id header before any
/// endpoint runs; the data layer reads it through <see cref="ITenantContext"/>.
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    /// <summary>Empty until the middleware sets it. An empty tenant sees no rows and cannot save any.</summary>
    public string TenantId { get; private set; } = "";

    public void SetTenant(string tenantId)
    {
        TenantId = tenantId;
    }
}
