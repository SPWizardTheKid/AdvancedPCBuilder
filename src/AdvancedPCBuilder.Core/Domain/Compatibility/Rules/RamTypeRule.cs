using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Domain.Compatibility.Rules;

/// <summary>
/// R6-RAM-TYPE: RAM type and module count must match the motherboard.
/// Formula: ram.MemoryType = mb.MemoryType AND ramQuantity ≤ mb.DimmSlots.
/// Each failed condition is a separate issue (both can appear). Severity: Error.
/// </summary>
public sealed class RamTypeRule : ICompatibilityRule
{
    public string Code => RuleCodes.RamType;

    public IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build)
    {
        MemoryType ramType = build.Ram.MemoryType;
        MemoryType boardType = build.Motherboard.MemoryType;

        if (ramType != boardType)
        {
            yield return new CompatibilityIssue(
                Code,
                Severity.Error,
                $"RAM type {ramType.ToString().ToUpperInvariant()} does not match motherboard memory type {boardType.ToString().ToUpperInvariant()}.");
        }

        int quantity = build.RamQuantity;
        int slots = build.Motherboard.DimmSlots;

        if (quantity > slots)
        {
            int difference = quantity - slots;
            yield return new CompatibilityIssue(
                Code,
                Severity.Error,
                $"{quantity} RAM modules selected but the motherboard has only {slots} DIMM slots ({difference} too many).",
                Actual: quantity,
                Limit: slots,
                Difference: difference);
        }
    }
}
