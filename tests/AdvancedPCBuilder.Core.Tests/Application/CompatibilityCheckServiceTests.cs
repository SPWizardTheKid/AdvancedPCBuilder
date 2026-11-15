using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Compatibility;
using AdvancedPCBuilder.Core.Tests.Fixtures;

namespace AdvancedPCBuilder.Core.Tests.Application;

public class CompatibilityCheckServiceTests
{
    [Fact]
    public async Task InvalidRequest_ReturnsErrors_AndNoRuleIsExecuted()
    {
        var countingRule = new CountingRule();
        var engine = new CompatibilityEngine([countingRule], TestEngine.Power());
        var service = new CompatibilityCheckService(new BuildResolver(new FakeComponentCatalog()), engine);

        ValidationResult<CompatibilityReport> result = await service.CheckAsync(TestRequests.S1 with { CpuId = null });

        Assert.False(result.IsValid);
        Assert.Null(result.Value);
        Assert.Equal(0, countingRule.Calls);
    }

    [Fact]
    public async Task RejectedBuild_IsAValidResult_NotAValidationError()
    {
        var service = new CompatibilityCheckService(new BuildResolver(new FakeComponentCatalog()), TestEngine.Create());

        ValidationResult<CompatibilityReport> result = await service.CheckAsync(TestRequests.S7);

        Assert.True(result.IsValid);
        Assert.Equal(CompatibilityStatus.Rejected, result.Value!.Status);
        Assert.Equal(600, result.Value.RequiredPsuWatts);
        Assert.Equal([RuleCodes.CoolerHeight, RuleCodes.PsuPower], result.Value.Issues.Select(i => i.RuleCode));
    }

    /// <summary>Test double: counts how often the engine called it.</summary>
    private sealed class CountingRule : ICompatibilityRule
    {
        public int Calls { get; private set; }

        public string Code => "TEST-COUNTER";

        public IEnumerable<CompatibilityIssue> Evaluate(BuildSpecification build)
        {
            Calls++;
            return [];
        }
    }
}
