using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Domain.Compatibility.Rules;

/// <summary>
/// R3-GPU-LENGTH: the GPU must fit, taking a front AIO radiator into account.
/// Formula: gpu.LengthMm ≤ case.MaxGpuLengthMm − radiatorThickness,
/// where radiatorThickness = aio.RadiatorThicknessMm for an AIO, otherwise 0.
/// limit = case.MaxGpuLengthMm − radiatorThickness; difference = gpu.LengthMm − limit.
/// Skipped when no GPU is selected. Severity: Error.
/// </summary>
public sealed class GpuLengthRule : ICompatibilityRule
{
    public string Code => RuleCodes.GpuLength;

    public IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build)
    {
        if (build.Gpu is null)
        {
            yield break;
        }

        int radiatorThickness = build.Cooler is AioCoolerSpec aio ? aio.RadiatorThicknessMm : 0;
        int length = build.Gpu.LengthMm;
        int limit = build.Case.MaxGpuLengthMm - radiatorThickness;

        if (length <= limit)
        {
            yield break;
        }

        int difference = length - limit;
        yield return new CompatibilityIssue(
            Code,
            Severity.Error,
            $"GPU length {length} mm exceeds the free space of {limit} mm (over by {difference} mm).",
            Actual: length,
            Limit: limit,
            Difference: difference);
    }
}
