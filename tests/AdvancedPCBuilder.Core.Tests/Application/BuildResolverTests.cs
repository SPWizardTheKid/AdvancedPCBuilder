using AdvancedPCBuilder.Core.Application;
using AdvancedPCBuilder.Core.Domain.Build;
using AdvancedPCBuilder.Core.Domain.Components;
using AdvancedPCBuilder.Core.Tests.Fixtures;
using static AdvancedPCBuilder.Core.Tests.Fixtures.TestComponents;

namespace AdvancedPCBuilder.Core.Tests.Application;

public class BuildResolverTests
{
    private readonly BuildResolver _resolver = new(new FakeComponentCatalog());

    [Fact]
    public async Task ValidRequest_IsMappedToCatalogSpecs()
    {
        ValidationResult<BuildSpecification> result = await _resolver.ResolveAsync(TestRequests.S1);

        Assert.True(result.IsValid);
        BuildSpecification build = result.Value!;
        Assert.Same(Cpu65Am5, build.Cpu);
        Assert.Same(MbMatxAm5, build.Motherboard);
        Assert.Same(RamDdr5_34, build.Ram);
        Assert.Equal(2, build.RamQuantity);
        Assert.Same(Gpu200_270, build.Gpu);
        Assert.Same(CoolAir155, build.Cooler);
        StorageItem drive = Assert.Single(build.Storage);
        Assert.Same(SsdM2, drive.Spec);
        Assert.Equal(1, drive.Quantity);
        Assert.Equal(3, build.CaseFanCount);
    }

    [Fact]
    public async Task OptionalGpuCoolerAndStorage_CanBeOmitted()
    {
        var request = TestRequests.S1 with { GpuId = null, CpuCoolerId = null, Storage = null, CaseFans = 0 };

        ValidationResult<BuildSpecification> result = await _resolver.ResolveAsync(request);

        Assert.True(result.IsValid);
        Assert.Null(result.Value!.Gpu);
        Assert.Null(result.Value.Cooler);
        Assert.Empty(result.Value.Storage);
    }

    [Fact]
    public async Task MissingRequiredComponents_GivesOneErrorPerField()
    {
        var request = TestRequests.S1 with { CpuId = null, MotherboardId = " ", Ram = null, CaseId = null, PsuId = "" };

        ValidationResult<BuildSpecification> result = await _resolver.ResolveAsync(request);

        Assert.False(result.IsValid);
        Assert.Null(result.Value);
        Assert.Equal(
            ["caseId", "cpuId", "motherboardId", "psuId", "ram"],
            result.Errors.Keys.Order());
    }

    [Fact]
    public async Task UnknownId_IsValidationError()
    {
        var request = TestRequests.S1 with { CpuId = "ts-cpu-does-not-exist" };

        ValidationResult<BuildSpecification> result = await _resolver.ResolveAsync(request);

        Assert.False(result.IsValid);
        Assert.Equal(["cpuId"], result.Errors.Keys);
    }

    [Fact]
    public async Task GpuIdInCpuField_IsWrongTypeError()
    {
        var request = TestRequests.S1 with { CpuId = "ts-gpu-200-270" };

        ValidationResult<BuildSpecification> result = await _resolver.ResolveAsync(request);

        Assert.False(result.IsValid);
        Assert.Equal(["cpuId"], result.Errors.Keys);
    }

    [Fact]
    public async Task AioId_IsAcceptedAsCooler()
    {
        var request = TestRequests.S1 with { CpuCoolerId = "ts-cool-aio-240" };

        ValidationResult<BuildSpecification> result = await _resolver.ResolveAsync(request);

        Assert.True(result.IsValid);
        Assert.IsType<AioCoolerSpec>(result.Value!.Cooler);
    }

    [Fact]
    public async Task RamQuantityBelowOne_IsValidationError()
    {
        var request = TestRequests.S1 with { Ram = new ComponentQuantity("ts-ram-ddr5-34", 0) };

        ValidationResult<BuildSpecification> result = await _resolver.ResolveAsync(request);

        Assert.Equal(["ram.quantity"], result.Errors.Keys);
    }

    [Fact]
    public async Task StorageQuantityBelowOne_AndUnknownStorageId_AreReportedPerItem()
    {
        var request = TestRequests.S1 with
        {
            Storage = [new ComponentQuantity("ts-ssd-m2-1tb", 0), new ComponentQuantity("ts-unknown", 1)]
        };

        ValidationResult<BuildSpecification> result = await _resolver.ResolveAsync(request);

        Assert.Equal(["storage[0].quantity", "storage[1].id"], result.Errors.Keys.Order());
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public async Task CaseFans_MustBeBetween0And10(int caseFans, bool expectedValid)
    {
        var request = TestRequests.S1 with { CaseFans = caseFans };

        ValidationResult<BuildSpecification> result = await _resolver.ResolveAsync(request);

        Assert.Equal(expectedValid, result.IsValid);
    }
}
