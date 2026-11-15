using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Tests.Fixtures;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Engine;

public class PowerCalculatorTests
{
    private readonly PowerCalculator _power = TestEngine.Power();

    [Fact]
    public void S1_Total345W_Required414W()
    {
        // 65 CPU + 50 MB + 2*5 RAM + 200 GPU + 8 M.2 + 3*(1 cooler fan + 3 case fans)
        var build = TestBuild.ValidBaseline().Build();

        Assert.Equal(345, _power.TotalWatts(build));
        Assert.Equal(414, _power.RequiredPsuWatts(build));
    }

    [Fact]
    public void SafetyMarginFromSettings_IsRespected()
    {
        // ceil(345 * 1.25) = ceil(431.25) = 432
        var power25 = new PowerCalculator(SpecPowerEstimates with { SafetyMarginPercent = 25 });

        Assert.Equal(432, power25.RequiredPsuWatts(TestBuild.ValidBaseline().Build()));
    }

    [Fact]
    public void Aio_AddsPumpAndRadiatorFans()
    {
        // 345 - 3 (air cooler fan) + 2*3 (AIO fans) + 5 (pump) = 353
        var build = TestBuild.ValidBaseline().WithCooler(CoolAio240).Build();

        Assert.Equal(353, _power.TotalWatts(build));
    }

    [Fact]
    public void NoGpuAndNoCooler_ContributeZero()
    {
        // 345 - 200 (GPU) - 3 (cooler fan) = 142
        var build = TestBuild.ValidBaseline().WithGpu(null).WithCooler(null).Build();

        Assert.Equal(142, _power.TotalWatts(build));
    }

    [Fact]
    public void StorageWatts_PerInterfaceTimesQuantity()
    {
        // 345 - 8 (baseline M.2) + 2*8 M.2 + 2*5 SATA SSD + 3*10 HDD = 393
        var build = TestBuild.ValidBaseline().WithStorage((SsdM2, 2), (SsdSata, 2), (Hdd, 3)).Build();

        Assert.Equal(393, _power.TotalWatts(build));
    }
}
