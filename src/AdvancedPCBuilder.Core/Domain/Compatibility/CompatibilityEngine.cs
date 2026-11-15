using AdvancedPCBuilder.Core.Domain.Build;

namespace AdvancedPCBuilder.Core.Domain.Compatibility;

/// <summary>
/// Runs ALL rules (it never stops at the first error), collects every issue
/// and derives the overall status (spec §3.1, §3.2).
/// </summary>
public sealed class CompatibilityEngine
{
    private readonly IReadOnlyList<ICompatibilityRule> _rules;
    private readonly PowerCalculator _power;

    public CompatibilityEngine(IEnumerable<ICompatibilityRule> rules, PowerCalculator power)
    {
        _rules = rules.ToList();
        _power = power;
    }

    public CompatibilityReport Check(BuildSpecification build)
    {
        var issues = new List<CompatibilityIssue>();
        foreach (ICompatibilityRule rule in _rules)
        {
            issues.AddRange(rule.Evaluate(build));
        }

        return new CompatibilityReport(
            DetermineStatus(issues),
            _power.RequiredPsuWatts(build),
            issues);
    }

    /// <summary>Any Error -> Rejected; else any Warning -> ApprovedWithWarnings; else Approved.</summary>
    public static CompatibilityStatus DetermineStatus(IEnumerable<CompatibilityIssue> issues)
    {
        var severities = issues.Select(issue => issue.Severity).ToList();

        if (severities.Contains(Severity.Error))
        {
            return CompatibilityStatus.Rejected;
        }

        if (severities.Contains(Severity.Warning))
        {
            return CompatibilityStatus.ApprovedWithWarnings;
        }

        return CompatibilityStatus.Approved;
    }
}
