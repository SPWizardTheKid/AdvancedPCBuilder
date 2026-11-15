using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Compatibility.Rules;
using AdvancedPCBuilder.Core.Domain.Components;
using AdvancedPCBuilder.Core.Tests.Fixtures;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Rules;

public class GpuLengthRuleTests
{
    private readonly GpuLengthRule _rule = new();

    [Fact]
    public void WithAirCooler_FullCaseLengthIsAvailable()
    {
        // 360 <= 360 - 0
        var gpu360 = new GpuSpec(LengthMm: 360, BoardPowerW: 200);
        var build = TestBuild.ValidBaseline().WithGpu(gpu360).WithCooler(CoolAir155).WithCase(CaseMid360).Build();

        Assert.Empty(_rule.Evaluate(build));
    }

    [Fact]
    public void WithAirCooler_OneMillimetreOverCaseLength_IsError()
    {
        var gpu361 = new GpuSpec(LengthMm: 361, BoardPowerW: 200);
        var build = TestBuild.ValidBaseline().WithGpu(gpu361).WithCooler(CoolAir155).WithCase(CaseMid360).Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(RuleCodes.GpuLength, issue.RuleCode);
        Assert.Equal(361, issue.Actual);
        Assert.Equal(360, issue.Limit);
        Assert.Equal(1, issue.Difference);
    }

    [Fact]
    public void WithAio_RadiatorThicknessReducesTheLimit()
    {
        // 309 > 360 - 52 = 308
        var build = TestBuild.ValidBaseline().WithGpu(Gpu220_309).WithCooler(CoolAio240).WithCase(CaseMid360).Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(308, issue.Limit);
    }

    [Fact]
    public void NoGpu_IsSkipped()
    {
        var build = TestBuild.ValidBaseline().WithGpu(null).Build();

        Assert.Empty(_rule.Evaluate(build));
    }
}
