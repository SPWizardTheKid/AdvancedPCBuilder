using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Domain.Compatibility.Rules;

/// <summary>
/// R2-COOLER-HEIGHT: an air cooler must fit inside the case.
/// Formula: air.HeightMm ≤ case.MaxCoolerHeightMm; difference = air.HeightMm − case.MaxCoolerHeightMm.
/// Applies to air coolers only (skipped for AIO or no cooler). Severity: Error.
/// </summary>
public sealed class CoolerHeightRule : ICompatibilityRule
{
    public string Code => RuleCodes.CoolerHeight;

    public IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build)
    {
        if (build.Cooler is not AirCoolerSpec air)
        {
            yield break;
        }

        int height = air.HeightMm;
        int limit = build.Case.MaxCoolerHeightMm;

        if (height <= limit)
        {
            yield break;
        }

        int difference = height - limit;
        yield return new CompatibilityIssue(
            Code,
            Severity.Error,
            $"CPU cooler height {height} mm exceeds the case limit of {limit} mm (over by {difference} mm).",
            Actual: height,
            Limit: limit,
            Difference: difference);
    }
}
