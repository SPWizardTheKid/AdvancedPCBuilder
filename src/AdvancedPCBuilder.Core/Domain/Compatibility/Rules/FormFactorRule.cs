using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Domain.Compatibility.Rules;

/// <summary>
/// R1-FORM-FACTOR: the case must support the motherboard form factor.
/// Formula: mb.FormFactor ∈ case.SupportedFormFactors. Severity: Error.
/// </summary>
public sealed class FormFactorRule : ICompatibilityRule
{
    public string Code => RuleCodes.FormFactor;

    public IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build)
    {
        FormFactor boardFormFactor = build.Motherboard.FormFactor;

        if (build.Case.SupportedFormFactors.Contains(boardFormFactor))
        {
            yield break;
        }

        string supported = string.Join(", ", build.Case.SupportedFormFactors.Select(f => f.ToDisplayName()));
        yield return new CompatibilityIssue(
            Code,
            Severity.Error,
            $"Motherboard form factor {boardFormFactor.ToDisplayName()} does not fit the selected case (supported: {supported}).");
    }
}
