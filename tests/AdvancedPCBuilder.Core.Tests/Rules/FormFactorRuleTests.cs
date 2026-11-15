using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Compatibility.Rules;
using AdvancedPCBuilder.Core.Tests.Fixtures;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Rules;

public class FormFactorRuleTests
{
    private readonly FormFactorRule _rule = new();

    [Fact]
    public void SupportedFormFactor_NoIssue()
    {
        var build = TestBuild.ValidBaseline().WithMotherboard(MbMatxAm5).WithCase(CaseMid360).Build();

        Assert.Empty(_rule.Evaluate(build));
    }

    [Fact]
    public void EAtxBoard_InCaseWithoutEAtxSupport_IsError()
    {
        var build = TestBuild.ValidBaseline().WithMotherboard(MbEatxAm5).WithCase(CaseMid360).Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(RuleCodes.FormFactor, issue.RuleCode);
        Assert.Equal(Severity.Error, issue.Severity);
    }

    [Fact]
    public void MicroAtxBoard_InMiniItxOnlyCase_IsError()
    {
        var build = TestBuild.ValidBaseline().WithMotherboard(MbMatxAm5).WithCase(CaseItx).Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(RuleCodes.FormFactor, issue.RuleCode);
    }
}
