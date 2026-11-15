namespace AdvancedPCBuilder.Core.Application;

/// <summary>
/// The tenant of the current request (port). The API sets it from the X-Tenant-Id header;
/// the data layer uses it to filter and guard every tenant-owned row.
/// </summary>
public interface ITenantContext
{
    string TenantId { get; }
}
