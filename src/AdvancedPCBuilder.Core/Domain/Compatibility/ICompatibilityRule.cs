using AdvancedPCBuilder.Core.Domain.Build;

namespace AdvancedPCBuilder.Core.Domain.Compatibility;

/// <summary>
/// One compatibility rule (Strategy pattern). Each rule checks one thing and returns zero or more
/// issues. New rules are added as new classes; the engine does not change (Open/Closed).
/// </summary>
public interface ICompatibilityRule
{
    /// <summary>Rule code from <see cref="RuleCodes"/>, e.g. "R5-PSU-POWER".</summary>
    string Code { get; }

    /// <summary>Returns the issues found in the build; empty when the rule passes or does not apply.</summary>
    IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build);
}
