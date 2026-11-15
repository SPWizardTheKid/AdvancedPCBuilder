using AdvancedPCBuilder.Core.Domain.Build;

namespace AdvancedPCBuilder.Core.Domain.Compatibility.Rules;

/// <summary>
/// R4-SOCKET: the CPU socket must match the motherboard socket.
/// Formula: cpu.Socket = mb.Socket. Severity: Error.
/// </summary>
public sealed class SocketRule : ICompatibilityRule
{
    public string Code => RuleCodes.Socket;

    public IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build)
    {
        string cpuSocket = build.Cpu.Socket;
        string boardSocket = build.Motherboard.Socket;

        if (cpuSocket == boardSocket)
        {
            yield break;
        }

        yield return new CompatibilityIssue(
            Code,
            Severity.Error,
            $"CPU socket {cpuSocket} does not match motherboard socket {boardSocket}.");
    }
}
