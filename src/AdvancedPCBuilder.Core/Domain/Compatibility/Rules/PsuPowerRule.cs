using AdvancedPCBuilder.Core.Domain.Build;

namespace AdvancedPCBuilder.Core.Domain.Compatibility.Rules;

/// <summary>
/// R5-PSU-POWER: the PSU must deliver the required power, with a safety margin.
/// required = ⌈P × (100 + SafetyMarginPercent) / 100⌉ (calculated by <see cref="PowerCalculator"/>).
/// Error:   required &gt; psu.Wattage; difference = required − psu.Wattage.
/// Warning: otherwise, if required × 100 &gt; psu.Wattage × (100 − WarningHeadroomPercent)
///          (less than 10 % left for upgrades). Equality with the PSU wattage is allowed (≤).
/// </summary>
public sealed class PsuPowerRule : ICompatibilityRule
{
    private readonly PowerCalculator _power;
    private readonly PowerEstimates _estimates;

    public PsuPowerRule(PowerCalculator power, PowerEstimates estimates)
    {
        _power = power;
        _estimates = estimates;
    }

    public string Code => RuleCodes.PsuPower;

    public IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build)
    {
        int required = _power.RequiredPsuWatts(build);
        int wattage = build.Psu.Wattage;
        int headroom = _estimates.WarningHeadroomPercent;

        if (required > wattage)
        {
            int difference = required - wattage;
            yield return new CompatibilityIssue(
                Code,
                Severity.Error,
                $"Required PSU power {required} W exceeds the selected PSU power {wattage} W (short by {difference} W).",
                Actual: required,
                Limit: wattage,
                Difference: difference);
        }
        else if (required * 100 > wattage * (100 - headroom))
        {
            yield return new CompatibilityIssue(
                Code,
                Severity.Warning,
                $"Required PSU power {required} W leaves less than {headroom} % headroom on the {wattage} W PSU (no room for upgrades).",
                Actual: required,
                Limit: wattage);
        }
    }
}
