using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Domain.Compatibility.Rules;

namespace AdvancedPCBuilder.Core.Tests.Fixtures;

/// <summary>Creates an engine with all 9 rules, wired the same way the application wires them.</summary>
public static class TestEngine
{
    public static CompatibilityEngine Create() => Create(TestComponents.SpecPowerEstimates);

    public static CompatibilityEngine Create(PowerEstimates estimates)
    {
        var power = new PowerCalculator(estimates);
        ICompatibilityRule[] rules =
        [
            new FormFactorRule(),
            new CoolerHeightRule(),
            new RadiatorMountRule(),
            new GpuLengthRule(),
            new SocketRule(),
            new PsuPowerRule(power, estimates),
            new RamTypeRule(),
            new RamClearanceRule(),
            new M2SataSharingRule()
        ];
        return new CompatibilityEngine(rules, power);
    }

    public static PowerCalculator Power() => new(TestComponents.SpecPowerEstimates);
}
