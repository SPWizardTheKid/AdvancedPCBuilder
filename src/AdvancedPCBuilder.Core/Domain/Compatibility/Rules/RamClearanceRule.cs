using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Domain.Compatibility.Rules;

/// <summary>
/// R7-RAM-CLEARANCE: RAM modules must fit under an air cooler.
/// Formula: ram.HeightMm ≤ air.RamClearanceMm; difference = ram.HeightMm − air.RamClearanceMm.
/// Applies to air coolers only (an AIO does not cover the RAM slots). Severity: Error.
/// </summary>
public sealed class RamClearanceRule : ICompatibilityRule
{
    public string Code => RuleCodes.RamClearance;

    public IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build)
    {
        if (build.Cooler is not AirCoolerSpec air)
        {
            yield break;
        }

        int height = build.Ram.HeightMm;
        int clearance = air.RamClearanceMm;

        if (height <= clearance)
        {
            yield break;
        }

        int difference = height - clearance;
        yield return new CompatibilityIssue(
            Code,
            Severity.Error,
            $"RAM module height {height} mm exceeds the cooler RAM clearance of {clearance} mm (over by {difference} mm).",
            Actual: height,
            Limit: clearance,
            Difference: difference);
    }
}
