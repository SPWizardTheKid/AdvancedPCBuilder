using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Components;
using AdvancedPCBuilder.Core.Tests.Fixtures;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Scenarios;

/// <summary>
/// Scenarios S1–S7 from spec §3.5 (mapping in STAGE2_PLAN.md §5), run through the full engine.
/// Every component is listed explicitly so each test reads like the scenario table.
/// </summary>
public class ScenarioTests
{
    private readonly CompatibilityEngine _engine = TestEngine.Create();
    private readonly PowerCalculator _power = TestEngine.Power();

    [Fact]
    public void S1_NormalBuild_IsApproved_Required414W()
    {
        BuildSpecification build = TestBuild.ValidBaseline()
            .WithCpu(Cpu65Am5)
            .WithMotherboard(MbMatxAm5)
            .WithRam(RamDdr5_34, 2)
            .WithGpu(Gpu200_270)
            .WithCooler(CoolAir155)
            .WithCase(CaseMid360)
            .WithPsu(Psu600)
            .WithStorage((SsdM2, 1))
            .WithCaseFans(3)
            .Build();

        CompatibilityReport report = _engine.Check(build);

        Assert.Equal(345, _power.TotalWatts(build));
        Assert.Equal(CompatibilityStatus.Approved, report.Status);
        Assert.Equal(414, report.RequiredPsuWatts);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void S2_AioWithGpuExactlyAtLimit_IsApproved_Required510W()
    {
        BuildSpecification build = S2Build(Gpu220_308);

        CompatibilityReport report = _engine.Check(build);

        // 308 <= 360 - 52 = 308 (equality allowed); R7 is skipped for an AIO, so 56 mm RAM is fine.
        Assert.Equal(425, _power.TotalWatts(build));
        Assert.Equal(CompatibilityStatus.Approved, report.Status);
        Assert.Equal(510, report.RequiredPsuWatts);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void S2Pair_GpuOneMillimetreTooLong_IsRejectedByR3()
    {
        CompatibilityReport report = _engine.Check(S2Build(Gpu220_309));

        Assert.Equal(CompatibilityStatus.Rejected, report.Status);
        CompatibilityIssue issue = Assert.Single(report.Issues);
        Assert.Equal(RuleCodes.GpuLength, issue.RuleCode);
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(309, issue.Actual);
        Assert.Equal(308, issue.Limit);
        Assert.Equal(1, issue.Difference);
    }

    [Fact]
    public void S3_RequiredPowerEqualsPsuWattage_IsApprovedWithOneR5Warning()
    {
        BuildSpecification build = S3Build(Gpu425_300);

        CompatibilityReport report = _engine.Check(build);

        // P = 625, required = ceil(625 * 1.2) = 750 <= 750 (no error), but 750 > 675 (90 % of 750).
        Assert.Equal(625, _power.TotalWatts(build));
        Assert.Equal(CompatibilityStatus.ApprovedWithWarnings, report.Status);
        Assert.Equal(750, report.RequiredPsuWatts);
        CompatibilityIssue issue = Assert.Single(report.Issues);
        Assert.Equal(RuleCodes.PsuPower, issue.RuleCode);
        Assert.Equal(Severity.Warning, issue.Severity);
    }

    [Fact]
    public void S3Pair_OneWattMore_IsRejectedByR5_ShortBy2W()
    {
        var gpu426W = new GpuSpec(LengthMm: 300, BoardPowerW: 426);
        BuildSpecification build = S3Build(gpu426W);

        CompatibilityReport report = _engine.Check(build);

        // P = 626, required = ceil(751.2) = 752 > 750.
        Assert.Equal(626, _power.TotalWatts(build));
        Assert.Equal(CompatibilityStatus.Rejected, report.Status);
        Assert.Equal(752, report.RequiredPsuWatts);
        CompatibilityIssue issue = Assert.Single(report.Issues);
        Assert.Equal(RuleCodes.PsuPower, issue.RuleCode);
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(752, issue.Actual);
        Assert.Equal(750, issue.Limit);
        Assert.Equal(2, issue.Difference);
    }

    [Fact]
    public void S4_EAtxBoardInMiniItxCase_IsRejectedWithOnlyR1()
    {
        BuildSpecification build = TestBuild.ValidBaseline()
            .WithCpu(Cpu65Am5)
            .WithMotherboard(MbEatxAm5)
            .WithRam(RamDdr5_34, 2)
            .WithGpu(Gpu200_270)
            .WithCooler(CoolAir155)
            .WithCase(CaseItx)
            .WithPsu(Psu600)
            .WithStorage((SsdM2, 1))
            .WithCaseFans(1)
            .Build();

        CompatibilityReport report = _engine.Check(build);

        Assert.Equal(CompatibilityStatus.Rejected, report.Status);
        CompatibilityIssue issue = Assert.Single(report.Issues);
        Assert.Equal(RuleCodes.FormFactor, issue.RuleCode);
        Assert.Equal(Severity.Error, issue.Severity);
    }

    [Fact]
    public void S5_TallRamUnderTowerCooler_IsRejectedByR7_CoolerAtHeightLimitIsOk()
    {
        BuildSpecification build = TestBuild.ValidBaseline()
            .WithCpu(Cpu120Am5)
            .WithMotherboard(MbAtxAm5)
            .WithRam(RamDdr5_56, 2)
            .WithGpu(Gpu200_270)
            .WithCooler(CoolAir165Tower)
            .WithCase(CaseMid360)
            .WithPsu(Psu750)
            .WithStorage((SsdM2, 1))
            .WithCaseFans(2)
            .Build();

        CompatibilityReport report = _engine.Check(build);

        Assert.Equal(CompatibilityStatus.Rejected, report.Status);
        CompatibilityIssue issue = Assert.Single(report.Issues);
        Assert.Equal(RuleCodes.RamClearance, issue.RuleCode);
        Assert.Equal(56, issue.Actual);
        Assert.Equal(32, issue.Limit);
        Assert.Equal(24, issue.Difference);
        // 165 <= 165: the cooler exactly at the case limit is NOT an R2 issue.
        Assert.DoesNotContain(report.Issues, i => i.RuleCode == RuleCodes.CoolerHeight);
    }

    [Fact]
    public void S6a_TwoM2DisableTwoSataPorts_FiveSataDrives_IsRejectedByR8()
    {
        CompatibilityReport report = _engine.Check(S6Build(sataSsdCount: 2, hddCount: 3));

        // disabled = {SATA5, SATA6}; available = 6 - 2 = 4; needed 5 > 4.
        Assert.Equal(CompatibilityStatus.Rejected, report.Status);
        CompatibilityIssue issue = Assert.Single(report.Issues);
        Assert.Equal(RuleCodes.M2Sata, issue.RuleCode);
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(5, issue.Actual);
        Assert.Equal(4, issue.Limit);
        Assert.Equal(1, issue.Difference);
    }

    [Fact]
    public void S6b_TwoM2DisableTwoSataPorts_FourSataDrives_IsApprovedWithR8Warning()
    {
        CompatibilityReport report = _engine.Check(S6Build(sataSsdCount: 1, hddCount: 3));

        Assert.Equal(CompatibilityStatus.ApprovedWithWarnings, report.Status);
        CompatibilityIssue issue = Assert.Single(report.Issues);
        Assert.Equal(RuleCodes.M2Sata, issue.RuleCode);
        Assert.Equal(Severity.Warning, issue.Severity);
        Assert.Equal(4, issue.Actual);
        Assert.Equal(4, issue.Limit);
    }

    [Fact]
    public void S7_TooTallCoolerAndWeakPsu_IsRejectedWithBothErrors()
    {
        BuildSpecification build = S7Build(CoolAir170, Psu450);

        CompatibilityReport report = _engine.Check(build);

        Assert.Equal(500, _power.TotalWatts(build));
        Assert.Equal(CompatibilityStatus.Rejected, report.Status);
        Assert.Equal(600, report.RequiredPsuWatts);
        // The engine does not stop at the first error: both issues are reported.
        Assert.Equal([RuleCodes.CoolerHeight, RuleCodes.PsuPower], report.Issues.Select(i => i.RuleCode));

        CompatibilityIssue cooler = report.Issues[0];
        Assert.Equal(Severity.Error, cooler.Severity);
        Assert.Equal(170, cooler.Actual);
        Assert.Equal(165, cooler.Limit);
        Assert.Equal(5, cooler.Difference);

        CompatibilityIssue psu = report.Issues[1];
        Assert.Equal(Severity.Error, psu.Severity);
        Assert.Equal(600, psu.Actual);
        Assert.Equal(450, psu.Limit);
        Assert.Equal(150, psu.Difference);
    }

    [Fact]
    public void S7Fixed_LowerCoolerAndBiggerPsu_IsApproved()
    {
        CompatibilityReport report = _engine.Check(S7Build(CoolAir155Dual, Psu750));

        Assert.Equal(CompatibilityStatus.Approved, report.Status);
        Assert.Equal(600, report.RequiredPsuWatts);
        Assert.Empty(report.Issues);
    }

    // ---- Scenario builds shared by a scenario and its paired test ----

    private static BuildSpecification S2Build(GpuSpec gpu) => TestBuild.ValidBaseline()
        .WithCpu(Cpu120Am5)
        .WithMotherboard(MbAtxAm5)
        .WithRam(RamDdr5_56, 2)
        .WithGpu(gpu)
        .WithCooler(CoolAio240)
        .WithCase(CaseMid360)
        .WithPsu(Psu750)
        .WithStorage((SsdM2, 1))
        .WithCaseFans(2)
        .Build();

    private static BuildSpecification S3Build(GpuSpec gpu) => TestBuild.ValidBaseline()
        .WithCpu(Cpu120Am5)
        .WithMotherboard(MbAtxAm5)
        .WithRam(RamDdr5_34, 2)
        .WithGpu(gpu)
        .WithCooler(CoolAir160Dual)
        .WithCase(CaseMid360)
        .WithPsu(Psu750)
        .WithStorage((SsdM2, 1))
        .WithCaseFans(2)
        .Build();

    private static BuildSpecification S6Build(int sataSsdCount, int hddCount) => TestBuild.ValidBaseline()
        .WithCpu(Cpu65Am5)
        .WithMotherboard(MbAtxAm5)
        .WithRam(RamDdr5_34, 2)
        .WithGpu(Gpu200_270)
        .WithCooler(CoolAir155)
        .WithCase(CaseMid360)
        .WithPsu(Psu750)
        .WithStorage((SsdM2, 2), (SsdSata, sataSsdCount), (Hdd, hddCount))
        .WithCaseFans(2)
        .Build();

    private static BuildSpecification S7Build(AirCoolerSpec cooler, PsuSpec psu) => TestBuild.ValidBaseline()
        .WithCpu(Cpu170Am5)
        .WithMotherboard(MbAtxAm5)
        .WithRam(RamDdr5_34, 2)
        .WithGpu(Gpu250_280)
        .WithCooler(cooler)
        .WithCase(CaseMid360)
        .WithPsu(psu)
        .WithStorage((SsdM2, 1))
        .WithCaseFans(2)
        .Build();
}
