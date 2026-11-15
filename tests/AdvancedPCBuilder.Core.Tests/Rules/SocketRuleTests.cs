using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Compatibility.Rules;
using AdvancedPCBuilder.Core.Tests.Fixtures;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Rules;

public class SocketRuleTests
{
    private readonly SocketRule _rule = new();

    [Fact]
    public void MatchingSockets_NoIssue()
    {
        var build = TestBuild.ValidBaseline().WithCpu(Cpu65Am5).WithMotherboard(MbAtxAm5).Build();

        Assert.Empty(_rule.Evaluate(build));
    }

    [Fact]
    public void Lga1700Cpu_OnAm5Board_IsError()
    {
        var build = TestBuild.ValidBaseline().WithCpu(Cpu65Lga1700).WithMotherboard(MbAtxAm5).Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(RuleCodes.Socket, issue.RuleCode);
        Assert.Equal(Severity.Error, issue.Severity);
    }
}
