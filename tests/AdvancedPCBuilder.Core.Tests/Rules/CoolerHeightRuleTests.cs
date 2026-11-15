using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Compatibility.Rules;
using AdvancedPCBuilder.Core.Tests.Fixtures;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Rules;

public class CoolerHeightRuleTests
{
    private readonly CoolerHeightRule _rule = new();

    [Fact]
    public void CoolerExactlyAtCaseLimit_NoIssue()
    {
        // 165 <= 165
        var build = TestBuild.ValidBaseline().WithCooler(CoolAir165Tower).WithCase(CaseMid360).Build();

        Assert.Empty(_rule.Evaluate(build));
    }

    [Fact]
    public void CoolerTallerThanCaseLimit_IsErrorWithNumbers()
    {
        var build = TestBuild.ValidBaseline().WithCooler(CoolAir170).WithCase(CaseMid360).Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(RuleCodes.CoolerHeight, issue.RuleCode);
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(170, issue.Actual);
        Assert.Equal(165, issue.Limit);
        Assert.Equal(5, issue.Difference);
    }

    [Fact]
    public void CoolerTallerThanCaseLimit_MessageContainsTheNumbers()
    {
        // The only test in the suite that checks a full message text.
        var build = TestBuild.ValidBaseline().WithCooler(CoolAir170).WithCase(CaseMid360).Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal("CPU cooler height 170 mm exceeds the case limit of 165 mm (over by 5 mm).", issue.Message);
    }

    [Fact]
    public void AioCooler_IsSkipped()
    {
        var build = TestBuild.ValidBaseline().WithCooler(CoolAio240).WithCase(CaseItx).Build();

        Assert.Empty(_rule.Evaluate(build));
    }

    [Fact]
    public void NoCooler_IsSkipped()
    {
        var build = TestBuild.ValidBaseline().WithCooler(null).Build();

        Assert.Empty(_rule.Evaluate(build));
    }
}
