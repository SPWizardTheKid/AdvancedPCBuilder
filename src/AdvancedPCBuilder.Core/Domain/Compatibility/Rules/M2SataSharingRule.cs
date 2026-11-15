using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Components;

namespace AdvancedPCBuilder.Core.Domain.Compatibility.Rules;

/// <summary>
/// R8-M2-SATA: M.2 slots may share lanes with SATA ports (spec §3.4 algorithm).
/// 1. m2Count &gt; mb.M2Slots.Count → Error (too many M.2 drives); the rule stops here.
/// 2. M.2 drives are assigned to slots in order: M2_1, M2_2, ...
/// 3. disabled = union of SharedSataPorts of the occupied slots.
/// 4. available = mb.SataPorts − |disabled|.
/// 5. sataCount &gt; available → Error; difference = sataCount − available.
/// 6. otherwise, disabled ≠ ∅ → Warning (tells which SATA ports are disabled).
/// m2Count = quantity of M2 drives; sataCount = quantity of SataSsd + Hdd drives.
/// </summary>
public sealed class M2SataSharingRule : ICompatibilityRule
{
    public string Code => RuleCodes.M2Sata;

    public IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build)
    {
        MotherboardSpec board = build.Motherboard;
        int m2Count = build.Storage
            .Where(item => item.Spec.Interface == StorageInterface.M2)
            .Sum(item => item.Quantity);
        int sataCount = build.Storage
            .Where(item => item.Spec.Interface is StorageInterface.SataSsd or StorageInterface.Hdd)
            .Sum(item => item.Quantity);

        // Step 1: not enough M.2 slots. Port sharing cannot be computed, so stop.
        if (m2Count > board.M2Slots.Count)
        {
            int extra = m2Count - board.M2Slots.Count;
            yield return new CompatibilityIssue(
                Code,
                Severity.Error,
                $"{m2Count} M.2 drives selected but the motherboard has only {board.M2Slots.Count} M.2 slots ({extra} too many).",
                Actual: m2Count,
                Limit: board.M2Slots.Count,
                Difference: extra);
            yield break;
        }

        // Steps 2-4: fill slots in order and collect the SATA ports they disable.
        List<M2SlotSpec> occupiedSlots = board.M2Slots.Take(m2Count).ToList();
        List<int> disabledPorts = occupiedSlots
            .SelectMany(slot => slot.SharedSataPorts)
            .Distinct()
            .Order()
            .ToList();
        int availableSata = board.SataPorts - disabledPorts.Count;

        string sharingText = "";
        if (disabledPorts.Count > 0)
        {
            string slotNames = string.Join(", ", occupiedSlots
                .Where(slot => slot.SharedSataPorts.Count > 0)
                .Select(slot => slot.Name));
            string portNames = string.Join(", ", disabledPorts.Select(port => $"SATA{port}"));
            sharingText = $"M.2 drive in slot {slotNames} disables {portNames}";
        }

        // Step 5: not enough SATA ports left.
        if (sataCount > availableSata)
        {
            int difference = sataCount - availableSata;
            string prefix = sharingText.Length > 0 ? sharingText + "; " : "";
            yield return new CompatibilityIssue(
                Code,
                Severity.Error,
                $"{prefix}{sataCount} SATA ports needed, {availableSata} available.",
                Actual: sataCount,
                Limit: availableSata,
                Difference: difference);
        }
        // Step 6: everything fits, but some SATA ports are disabled.
        else if (disabledPorts.Count > 0)
        {
            yield return new CompatibilityIssue(
                Code,
                Severity.Warning,
                $"{sharingText} ({availableSata} SATA ports remain, {sataCount} used).",
                Actual: sataCount,
                Limit: availableSata);
        }
    }
}
