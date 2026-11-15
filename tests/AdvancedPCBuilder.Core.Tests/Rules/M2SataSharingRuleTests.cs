using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Compatibility.Rules;
using AdvancedPCBuilder.Core.Tests.Fixtures;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Rules;

/// <summary>
/// mATX / ATX boards: 6 SATA ports, M2_1 shares nothing, M2_2 shares SATA5-6.
/// E-ATX board: 8 SATA ports, M2_1 and M2_2 share nothing, M2_3 shares SATA7-8.
/// </summary>
public class M2SataSharingRuleTests
{
    private readonly M2SataSharingRule _rule = new();

    [Fact]
    public void ThreeM2Drives_OnTwoSlotBoard_IsSingleErrorAndRuleStops()
    {
        // 7 SATA drives would also be too many, but the rule stops after the M.2 error.
        var build = TestBuild.ValidBaseline()
            .WithMotherboard(MbAtxAm5)
            .WithStorage((SsdM2, 3), (Hdd, 7))
            .Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(RuleCodes.M2Sata, issue.RuleCode);
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(3, issue.Actual);
        Assert.Equal(2, issue.Limit);
        Assert.Equal(1, issue.Difference);
    }

    [Fact]
    public void OneM2InFirstSlot_AllSixSataPortsUsable_NoIssue()
    {
        var build = TestBuild.ValidBaseline()
            .WithMotherboard(MbAtxAm5)
            .WithStorage((SsdM2, 1), (SsdSata, 3), (Hdd, 3))
            .Build();

        Assert.Empty(_rule.Evaluate(build));
    }

    [Fact]
    public void SevenSataDrives_WithoutSharing_IsError()
    {
        var build = TestBuild.ValidBaseline()
            .WithMotherboard(MbAtxAm5)
            .WithStorage((SsdM2, 1), (Hdd, 7))
            .Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(7, issue.Actual);
        Assert.Equal(6, issue.Limit);
        Assert.Equal(1, issue.Difference);
    }

    [Fact]
    public void SecondM2Slot_WithNoSataDrives_IsWarning()
    {
        var build = TestBuild.ValidBaseline()
            .WithMotherboard(MbAtxAm5)
            .WithStorage((SsdM2, 2))
            .Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(Severity.Warning, issue.Severity);
        Assert.Equal(0, issue.Actual);
        Assert.Equal(4, issue.Limit);
    }

    [Fact]
    public void EAtx_ThirdM2Slot_DisablesSata7And8_SixSataDrivesIsWarning()
    {
        // available = 8 - 2 = 6; 6 <= 6
        var build = TestBuild.ValidBaseline()
            .WithMotherboard(MbEatxAm5)
            .WithStorage((SsdM2, 3), (Hdd, 6))
            .Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(Severity.Warning, issue.Severity);
        Assert.Equal(6, issue.Limit);
    }

    [Fact]
    public void EAtx_ThirdM2Slot_DisablesSata7And8_SevenSataDrivesIsError()
    {
        var build = TestBuild.ValidBaseline()
            .WithMotherboard(MbEatxAm5)
            .WithStorage((SsdM2, 3), (Hdd, 7))
            .Build();

        CompatibilityIssue issue = Assert.Single(_rule.Evaluate(build));
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(7, issue.Actual);
        Assert.Equal(6, issue.Limit);
        Assert.Equal(1, issue.Difference);
    }
}
