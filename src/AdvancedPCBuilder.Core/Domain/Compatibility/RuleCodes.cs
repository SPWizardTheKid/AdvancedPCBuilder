namespace AdvancedPCBuilder.Core.Domain.Compatibility;

/// <summary>Rule codes exactly as in spec §3.4. Tests and API clients rely on these strings.</summary>
public static class RuleCodes
{
    public const string FormFactor = "R1-FORM-FACTOR";
    public const string CoolerHeight = "R2-COOLER-HEIGHT";
    public const string RadiatorMount = "R2b-RADIATOR-MOUNT";
    public const string GpuLength = "R3-GPU-LENGTH";
    public const string Socket = "R4-SOCKET";
    public const string PsuPower = "R5-PSU-POWER";
    public const string RamType = "R6-RAM-TYPE";
    public const string RamClearance = "R7-RAM-CLEARANCE";
    public const string M2Sata = "R8-M2-SATA";
}
