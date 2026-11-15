using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Compatibility.Rules;
using AdvancedPCBuilder.Core.Domain.Components;
using AdvancedPCBuilder.Core.Tests.Fixtures;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Rules;

/// <summary>
/// Baseline (S1) is 345 W with a 200 W GPU and a 600 W PSU. Changing only the GPU power moves P 1:1.
/// Warning threshold for 600 W: required * 100 > 600 * 90, i.e. required > 540.
/// </summary>
public class PsuPowerRuleTests
{
    private readonly PsuPowerRule _rule = new(TestEngine.Power(), SpecPowerEstimates);

    [Fact]
    public void PlentyOfHeadroom_NoIssue()
    {
        var build = TestBuild.ValidBaseline().Build();   // required 414 <= 540

        Assert.Empty(_rule.Evaluate(build));
    }

    [Fact]
    public void RequiredExactlyAtNinetyPercent_NoWarning()
    {
        // P = 345 - 200 + 305 = 450 -> required = 540; 540 * 100 = 54000 is not > 54000.
        var build = TestBuild.ValidBaseline().WithGpu(new GpuSpec(LengthMm: 270, BoardPowerW: 305)).Build();

        Assert.Empty(_rule.Evaluate(build));
    }

    [Fact]
    public void RequiredJustAboveNinetyPercent_IsWarning()
    {
        // P = 451 -> required = ceil(541.2) = 542 > 540.
        var build = TestBuild.ValidBaseline().WithGpu(new GpuSpec(LengthMm: 270, BoardPowerW: 306)).Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(RuleCodes.PsuPower, issue.RuleCode);
        Assert.Equal(Severity.Warning, issue.Severity);
        Assert.Equal(542, issue.Actual);
        Assert.Equal(600, issue.Limit);
    }

    [Fact]
    public void RequiredAbovePsuWattage_IsErrorOnly()
    {
        var build = TestBuild.ValidBaseline().WithPsu(Psu450).WithGpu(Gpu425_300).Build();

        // P = 345 - 200 + 425 = 570 -> required = 684 > 450. Only an Error, not also a Warning.
        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(684, issue.Actual);
        Assert.Equal(450, issue.Limit);
        Assert.Equal(234, issue.Difference);
    }
}
