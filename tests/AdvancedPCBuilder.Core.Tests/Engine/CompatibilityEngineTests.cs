using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Tests.Fixtures;

namespace AdvancedPCBuilder.Core.Tests.Engine;

/// <summary>Engine behaviour tested with fake rules, independent of the real rule logic.</summary>
public class CompatibilityEngineTests
{
    private static readonly CompatibilityIssue AnError = new("TEST-ERROR", Severity.Error, "error");
    private static readonly CompatibilityIssue AWarning = new("TEST-WARNING", Severity.Warning, "warning");

    private static CompatibilityReport Run(params ICompatibilityRule[] rules)
    {
        var engine = new CompatibilityEngine(rules, TestEngine.Power());
        return engine.Check(TestBuild.ValidBaseline().Build());
    }

    [Fact]
    public void NoIssues_IsApproved()
    {
        CompatibilityReport report = Run(new FixedRule(), new FixedRule());

        Assert.Equal(CompatibilityStatus.Approved, report.Status);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void OnlyWarnings_IsApprovedWithWarnings()
    {
        CompatibilityReport report = Run(new FixedRule(AWarning));

        Assert.Equal(CompatibilityStatus.ApprovedWithWarnings, report.Status);
    }

    [Fact]
    public void ErrorAndWarning_IsRejected()
    {
        CompatibilityReport report = Run(new FixedRule(AWarning), new FixedRule(AnError));

        Assert.Equal(CompatibilityStatus.Rejected, report.Status);
    }

    [Fact]
    public void RunsEveryRule_AndKeepsAllIssuesInRuleOrder()
    {
        CompatibilityReport report = Run(new FixedRule(AnError), new FixedRule(AWarning, AnError), new FixedRule());

        Assert.Equal(["TEST-ERROR", "TEST-WARNING", "TEST-ERROR"], report.Issues.Select(i => i.RuleCode));
    }

    [Fact]
    public void Report_ContainsRequiredPsuWattsFromPowerCalculator()
    {
        CompatibilityReport report = Run();

        Assert.Equal(414, report.RequiredPsuWatts);   // S1 baseline
    }

    /// <summary>Test double: a rule that always returns the given issues.</summary>
    private sealed class FixedRule : ICompatibilityRule
    {
        private readonly CompatibilityIssue[] _issues;

        public FixedRule(params CompatibilityIssue[] issues)
        {
            _issues = issues;
        }

        public string Code => "TEST";

        public IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build) => _issues;
    }
}
