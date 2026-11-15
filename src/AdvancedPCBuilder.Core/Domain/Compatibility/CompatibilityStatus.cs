namespace AdvancedPCBuilder.Core.Domain.Compatibility;

/// <summary>Overall result of a compatibility check (spec §3.1).</summary>
public enum CompatibilityStatus
{
    /// <summary>No issues at all.</summary>
    Approved,

    /// <summary>No errors, at least one warning.</summary>
    ApprovedWithWarnings,

    /// <summary>At least one error.</summary>
    Rejected
}
