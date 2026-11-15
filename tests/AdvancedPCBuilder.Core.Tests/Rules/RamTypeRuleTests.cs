using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Compatibility.Rules;
using AdvancedPCBuilder.Core.Tests.Fixtures;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Rules;

public class RamTypeRuleTests
{
    private readonly RamTypeRule _rule = new();

    [Fact]
    public void MatchingTypeAndAllSlotsFilled_NoIssue()
    {
        // 4 <= 4 DIMM slots
        var build = TestBuild.ValidBaseline().WithRam(RamDdr5_34, 4).Build();

        Assert.Empty(_rule.Evaluate(build));
    }

    [Fact]
    public void Ddr4Ram_OnDdr5Board_IsError()
    {
        var build = TestBuild.ValidBaseline().WithRam(RamDdr4_32, 2).Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(RuleCodes.RamType, issue.RuleCode);
        Assert.Equal(Severity.Error, issue.Severity);
    }

    [Fact]
    public void SixModules_OnFourSlotBoard_IsErrorWithNumbers()
    {
        var build = TestBuild.ValidBaseline().WithRam(RamDdr5_34, 6).Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(RuleCodes.RamType, issue.RuleCode);
        Assert.Equal(6, issue.Actual);
        Assert.Equal(4, issue.Limit);
        Assert.Equal(2, issue.Difference);
    }

    [Fact]
    public void WrongTypeAndTooManyModules_GivesTwoSeparateIssues()
    {
        var build = TestBuild.ValidBaseline().WithRam(RamDdr4_32, 6).Build();

        var issues = _rule.Evaluate(build).ToList();

        Assert.Equal(2, issues.Count);
        Assert.All(issues, issue => Assert.Equal(RuleCodes.RamType, issue.RuleCode));
    }
}
