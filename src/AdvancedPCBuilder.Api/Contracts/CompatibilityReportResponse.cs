using AdvancedPCBuilder.Core.Domain.Compatibility;

namespace AdvancedPCBuilder.Api.Contracts;

/// <summary>
/// The compatibility report as JSON, in the exact shape of spec §3.1:
/// { "status", "requiredPsuWatts", "issues": [ { "rule", "severity", "message", "actual", "limit", "difference" } ] }.
/// A separate DTO keeps JSON naming out of the Core domain records.
/// </summary>
public sealed record CompatibilityReportResponse(
    CompatibilityStatus Status,
    int RequiredPsuWatts,
    IReadOnlyList<IssueResponse> Issues)
{
    public static CompatibilityReportResponse From(CompatibilityReport report) => new(
        report.Status,
        report.RequiredPsuWatts,
        report.Issues.Select(IssueResponse.From).ToList());
}

/// <summary>One issue. <c>Rule</c> is the rule code, e.g. "R5-PSU-POWER" (named "rule" as in the spec JSON).</summary>
public sealed record IssueResponse(
    string Rule,
    Severity Severity,
    string Message,
    int? Actual,
    int? Limit,
    int? Difference)
{
    public static IssueResponse From(CompatibilityIssue issue) => new(
        issue.RuleCode,
        issue.Severity,
        issue.Message,
        issue.Actual,
        issue.Limit,
        issue.Difference);
}
