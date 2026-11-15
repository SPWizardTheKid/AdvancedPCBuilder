using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Compatibility.Rules;
using AdvancedPCBuilder.Core.Domain.Components;
using AdvancedPCBuilder.Core.Tests.Fixtures;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Rules;

public class RamClearanceRuleTests
{
    private readonly RamClearanceRule _rule = new();

    [Fact]
    public void RamExactlyAtClearance_NoIssue()
    {
        // 40 <= 40
        var ram40 = new RamSpec(MemoryType.Ddr5, HeightMm: 40);
        var build = TestBuild.ValidBaseline().WithRam(ram40, 2).WithCooler(CoolAir155).Build();

        Assert.Empty(_rule.Evaluate(build));
    }

    [Fact]
    public void RamOneMillimetreTooTall_IsError()
    {
        var ram41 = new RamSpec(MemoryType.Ddr5, HeightMm: 41);
        var build = TestBuild.ValidBaseline().WithRam(ram41, 2).WithCooler(CoolAir155).Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(RuleCodes.RamClearance, issue.RuleCode);
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(41, issue.Actual);
        Assert.Equal(40, issue.Limit);
        Assert.Equal(1, issue.Difference);
    }

    [Fact]
    public void AioCooler_IsSkipped_EvenForTallRam()
    {
        var build = TestBuild.ValidBaseline().WithRam(RamDdr5_56, 2).WithCooler(CoolAio240).Build();

        Assert.Empty(_rule.Evaluate(build));
    }
}
