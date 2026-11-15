namespace AdvancedPCBuilder.Infrastructure.Entities;

/// <summary>A saved build of one tenant: the request and its latest report, both stored as JSON.</summary>
public sealed class SavedBuildEntity : ITenantOwned
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = "";
    public string Name { get; set; } = "";
    public string RequestJson { get; set; } = "";
    public string ReportJson { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
}
