namespace AdvancedPCBuilder.Core.Domain.Components;

/// <summary>Motherboard form factor (spec §3.3).</summary>
public enum FormFactor
{
    EAtx,
    Atx,
    MicroAtx,
    MiniItx
}

public static class FormFactorExtensions
{
    /// <summary>Human-readable name used in rule messages, e.g. "E-ATX".</summary>
    public static string ToDisplayName(this FormFactor formFactor) => formFactor switch
    {
        FormFactor.EAtx => "E-ATX",
        FormFactor.Atx => "ATX",
        FormFactor.MicroAtx => "mATX",
        FormFactor.MiniItx => "Mini-ITX",
        _ => formFactor.ToString()
    };
}
