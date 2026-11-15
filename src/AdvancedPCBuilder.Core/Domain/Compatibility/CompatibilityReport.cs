namespace AdvancedPCBuilder.Core.Domain.Compatibility;

/// <summary>Result of checking one build: overall status, required PSU power and all issues found.</summary>
public sealed record CompatibilityReport(
    CompatibilityStatus Status,
    int RequiredPsuWatts,
    IReadOnlyList<CompatibilityIssue> Issues);
