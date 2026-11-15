using AdvancedPCBuilder.Core.Domain.Compatibility;

namespace AdvancedPCBuilder.Core.Application;

/// <summary>
/// A build saved by a tenant, with the report from its latest check.
/// There is no TenantId here: the data layer assigns and enforces it from <see cref="ITenantContext"/>.
/// </summary>
public sealed record SavedBuild(
    Guid Id,
    string Name,
    BuildRequest Request,
    CompatibilityReport Report,
    DateTimeOffset UpdatedAt);
