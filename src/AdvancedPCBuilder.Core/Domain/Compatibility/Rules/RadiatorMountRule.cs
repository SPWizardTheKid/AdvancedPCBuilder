using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Domain.Compatibility.Rules;

/// <summary>
/// R2b-RADIATOR-MOUNT: an AIO radiator must fit the front radiator mount of the case.
/// Formula: aio.RadiatorSizeMm ∈ case.SupportedFrontRadiatorsMm.
/// Applies to AIO coolers only (skipped for air or no cooler). Severity: Error.
/// </summary>
public sealed class RadiatorMountRule : ICompatibilityRule
{
    public string Code => RuleCodes.RadiatorMount;

    public IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build)
    {
        if (build.Cooler is not AioCoolerSpec aio)
        {
            yield break;
        }

        IReadOnlyList<int> supportedSizes = build.Case.SupportedFrontRadiatorsMm;

        if (supportedSizes.Contains(aio.RadiatorSizeMm))
        {
            yield break;
        }

        string supported = supportedSizes.Count == 0
            ? "none"
            : string.Join(", ", supportedSizes.Select(size => $"{size} mm"));
        yield return new CompatibilityIssue(
            Code,
            Severity.Error,
            $"AIO radiator {aio.RadiatorSizeMm} mm cannot be mounted at the front of the case (supported: {supported}).",
            Actual: aio.RadiatorSizeMm);
    }
}
