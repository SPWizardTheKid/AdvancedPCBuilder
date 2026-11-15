namespace AdvancedPCBuilder.Core.Domain.Compatibility;

public enum Severity
{
    /// <summary>The build works, but the user should know about a limitation.</summary>
    Warning,

    /// <summary>The build cannot work; it is rejected.</summary>
    Error
}
