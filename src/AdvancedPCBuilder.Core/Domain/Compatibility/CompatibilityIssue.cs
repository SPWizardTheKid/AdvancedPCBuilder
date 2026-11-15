namespace AdvancedPCBuilder.Core.Domain.Compatibility;

/// <summary>
/// One rule violation. The numbers let tests (and users) see exactly how far off the build is.
/// </summary>
/// <param name="Actual">The build's value, e.g. cooler height 170 (mm) or required PSU power 600 (W).</param>
/// <param name="Limit">The allowed value, e.g. case limit 165 (mm) or PSU wattage 450 (W).</param>
/// <param name="Difference">How much the limit is exceeded (Actual - Limit), when that makes sense.</param>
public sealed record CompatibilityIssue(
    string RuleCode,
    Severity Severity,
    string Message,
    int? Actual = null,
    int? Limit = null,
    int? Difference = null);
