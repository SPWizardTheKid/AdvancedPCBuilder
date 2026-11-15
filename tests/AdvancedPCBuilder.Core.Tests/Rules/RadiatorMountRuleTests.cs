using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Compatibility.Rules;
using AdvancedPCBuilder.Core.Tests.Fixtures;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Rules;

public class RadiatorMountRuleTests
{
    private readonly RadiatorMountRule _rule = new();

    [Fact]
    public void SupportedRadiatorSize_NoIssue()
    {
        var build = TestBuild.ValidBaseline().WithCooler(CoolAio240).WithCase(CaseMid360).Build();

        Assert.Empty(_rule.Evaluate(build));
    }

    [Fact]
    public void Aio240_InCaseWithoutFrontRadiatorMount_IsError()
    {
        var build = TestBuild.ValidBaseline().WithCooler(CoolAio240).WithCase(CaseItx).Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(RuleCodes.RadiatorMount, issue.RuleCode);
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(240, issue.Actual);
    }

    [Fact]
    public void AirCooler_IsSkipped()
    {
        var build = TestBuild.ValidBaseline().WithCooler(CoolAir155).WithCase(CaseItx).Build();

        Assert.Empty(_rule.Evaluate(build));
    }
}
